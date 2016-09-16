task :default do
end



### Tooling to build dependency graph

DIR = File.expand_path(File.dirname(__FILE__))
DIR_REGEX = Regexp.new("^" + Regexp.escape(DIR + "/"))
def normalize_path(path)
  path.gsub!("\\", "/")
  return (File.expand_path path).sub(DIR_REGEX, "")
end

def process_sln_file(env, filepath)
  solution_file_dir = File.dirname(normalize_path(filepath))

  process = lambda do |line|
    if /(?<csproj>[^"]+\.csproj)/ =~ line
      process_csproj_file(env, solution_file_dir + "/" + normalize_path(csproj))
    end
  end

  File.open(filepath, 'r') do |f|
    while line = f.gets
      process.call line
    end
  end
end

def artifact_pointer(project_filename)
  p = "pointer: #{project_filename}"
  puts p
  return p
end

def process_csproj_file(env, csproj_filename)
  csproj_root = File.dirname csproj_filename
  pointer = artifact_pointer csproj_filename

  # metadata to collect
  assembly_name = nil
  output_type = nil
  assembly_file = nil
  source_files = []
  project_references = []
  depends_on_xunit = false

  in_debug = false
  File.open(csproj_filename, 'r') do |fp|
    Xml.new
      .tag_end("Project/PropertyGroup/AssemblyName", lambda {|value| assembly_name = value })
      .tag_end("Project/PropertyGroup/OutputType", lambda {|value| output_type = value })
      .tag_start("Project/PropertyGroup", lambda {|attrs| in_debug = /Debug/ =~ attrs['Condition'] })
      .tag_end("Project/PropertyGroup/OutputPath", lambda {|value| assembly_file = normalize_path "#{csproj_root}/#{value}#{assembly_name}.#{if output_type == "Exe" then "exe" else "dll" end}" if in_debug })
      .tag_start("Project/ItemGroup/Compile", lambda {|attrs| source_files.push(normalize_path "#{csproj_root}/#{attrs['Include']}") })
      .tag_start("Project/ItemGroup/ProjectReference", lambda {|attrs| project_references.push(normalize_path "#{csproj_root}/#{attrs['Include']}") })
      .tag_start("Project/ItemGroup/Reference", lambda {|attrs| depends_on_xunit = true if attrs['Include'].start_with? 'xunit.core,' })
      .parse fp
  end

  desc "#{assembly_file}#{if depends_on_xunit then " (TEST)" else "" end}"
  task pointer => assembly_file

  file csproj_filename

  file assembly_file => csproj_filename do
    env.builder.build_project(csproj_filename)
  end

  source_files.each do |source_file|
    file source_file
    file assembly_file => source_file
  end

  project_references.each do |reference_project_filename|
    puts "#{csproj_filename} -> #{reference_project_filename}"
    task pointer => artifact_pointer(reference_project_filename)
  end
end


### XML Parsing for .csproj files

class Xml
  def initialize()
    @stack = []
    @tag_start_trails = {}
    @tag_end_trails = {}
  end

  def tag_start(trail, block)
    @tag_start_trails[trail] = [] if @tag_start_trails[trail] == nil
    @tag_start_trails[trail].push block

    return self
  end

  def tag_end(trail, block)
    @tag_end_trails[trail] = [] if @tag_end_trails[trail] == nil
    @tag_end_trails[trail].push block

    return self
  end

  def self.stack_to_trail(stack)
    stack.join("/")
  end

  def self.parse_attrs(tag)
    hash = {}

    tag.sub!(/^[^ ]+ +/, "")
    tag.strip!

    key_buffer = ""
    val_buffer = ""
    in_key = true
    in_val = false

    add_to_buffer = lambda do |c|
      if in_key
        c.strip!
        key_buffer << c
      elsif in_val
        val_buffer << c
      end
    end

    tag.each_char do |c|
      if not in_key and not in_val
        c.strip!
        next if c.empty?
      end

      case c
      when "="
        if in_key
          in_key = false
        else
          add_to_buffer.call c
        end
      when '"'
        if not in_key and not in_val
          in_val = true
        elsif in_val
          in_val = false
          hash[key_buffer] = val_buffer
          key_buffer = ""
          val_buffer = ""
        else
          add_to_buffer.call c
        end
      else
        if not in_key and not in_val
          in_key = true
        end

        add_to_buffer.call c
      end
    end

    return hash
  end

  def parse(stream)
    tag_buffer = ""
    value_buffer = ""
    in_tag = false

    add_to_buffer = lambda do |c|
      if in_tag
        tag_buffer << c
      elsif not "\r\n".include? c
        value_buffer << c
      end
    end

    stream.each_char do |c|
      case c
      when "<"
        in_tag = true
      when ">"
        if tag_buffer.start_with? "?xml"
          in_tag = false
          tag_buffer = ""
          next
        end

        if tag_buffer.start_with? "!--"
          if not tag_buffer.end_with? "--"
            add_to_buffer.call c
            next
          end
        elsif tag_buffer.start_with? "/"
          if @stack.last != tag_buffer[1..-1]
            raise "Expecting end tag for <#{@stack.last}>, found <#{tag_buffer}>"
          end

          call_end_hooks(Xml.stack_to_trail(@stack), value_buffer)
          @stack.pop
        elsif tag_buffer.end_with? "/"
          call_start_hooks(Xml.stack_to_trail(@stack + [tag_buffer.sub(/[ \/].*/, "")]), Xml.parse_attrs(tag_buffer))
        else
          @stack.push tag_buffer.sub(/ .*/, "")
          call_start_hooks(Xml.stack_to_trail(@stack), Xml.parse_attrs(tag_buffer))
        end

        in_tag = false
        tag_buffer = ""
        value_buffer = ""
      else
        add_to_buffer.call c
      end
    end

    if not @stack.empty?
      raise "Tags left on stack: #{Xml.stack_to_trail @stack}"
    end
  end

  def call_start_hooks(trail, attrs)
    blocks = @tag_start_trails[trail]
    return if blocks == nil

    blocks.each do |b|
      b.call attrs
    end
  end

  def call_end_hooks(trail, value)
    blocks = @tag_end_trails[trail]
    return if blocks == nil

    blocks.each do |b|
      b.call value
    end
  end
end


### Environment

def which(cmd)
  exts = ENV['PATHEXT'] ? ENV['PATHEXT'].split(';') : ['']
  ENV['PATH'].split(File::PATH_SEPARATOR).each do |path|
    exts.each do |ext|
      exe = File.join(path, "#{cmd}#{ext}")
      return exe if File.executable?(exe) && !File.directory?(exe)
    end
  end
  return nil
end

class Build
  def build_project(csproj_path)
    raise "Not implemented"
  end
end

class MSBuild < Build
  def initialize()
    @msbuild = which "msbuild"
    @msbuild = FileList.new(normalize_path "#{ENV['windir']}/Microsoft.NET/Framework/**/MSBuild.exe").last if @msbuild == nil
    raise "Can't find MSBuild" if @msbuild == nil
  end

  def build_project(csproj_path)
    puts "#{@msbuild} /nologo /m:4 /v:quiet /clp:Summary /t:Debug #{csproj_path}"
  end
end

class XBuild < Build
  def initialize()
    @xbuild = which "xbuild"
    raise "Can't find XBuild" if @xbuild == nil
  end
end

class Env
  def self.construct()
    ENV['windir'] == nil ? Posix.new : Win.new
  end

  def nuget()
    nuget = FileList.new("**/nuget.exe").last
    nuget = which "nuget" if nuget == nil
    return nuget
  end

  def builder()
    raise "Not implemented"
  end

  def xunit()
    FileList.new("**/xunit.console.exe").last
  end
end

class Win < Env
  def builder()
    MSBuild.new
  end
end

class Posix < Env
  def builder()
    XBuild.new
  end
end

### Initialization


env = Env.construct
puts "nuget: #{env.nuget}"
puts "builder: #{env.builder}"
puts "xunit: #{env.xunit}"

FileList.new("**/*.sln").each do |sln|
  process_sln_file(env, sln)
end
