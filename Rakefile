desc "Build and test everything, as needed"
task :default => :test

desc "Run tests, as needed"
task :test => :build

desc "Build everything, as needed"
task :build

desc "Remove all assemblies and test notes"
task :clean

desc "Push to git after testing"
task :push => :test do
  git = which "git"
  if git == nil
    ERRORS.push "Install git to use this task"
  elsif not ERRORS.empty?
    ERRORS.push "Not pushing due to errors/failing tests"
  else
    git_status = `git -c color.status=always status`
    if /^nothing to commit, working (directory|tree) clean$/ !~ git_status
      ERRORS.push "Refusing to push, working directory is not clean:\n\n#{git_status}"
      next
    end

    begin
      verbose(false) { sh "git -c color.status=always push" }
    rescue => e
      ERRORS.push "git failed: #{e}"
    end
  end
end

ERRORS = []
at_exit do
  exit 0 if ERRORS.empty?

  ERRORS.each { |err| STDERR.puts err }
  exit 1
end

### Tooling to build dependency graph

DIR = File.expand_path(File.dirname(__FILE__))
DIR_REGEX = Regexp.new("^" + Regexp.escape(DIR + "/"))
def normalize_path(path)
  path.gsub!("\\", "/")
  return (File.expand_path path).sub(DIR_REGEX, "")
end

class System
  include Rake::DSL

  def initialize(env)
    @env = env

    @project_dependency_map = {}
    @project_to_artifact_map = {}
  end

  def process_sln(sln_path)
    sln_dir_path = File.dirname(sln_path)

    @env.nuget "restore #{sln_path}"

    process = lambda do |line|
      if /(?<csproj>[^"]+\.csproj)/ =~ line
        process_csproj(sln_dir_path + "/" + normalize_path(csproj))
      end
    end

    File.open(sln_path, 'r') do |f|
      while line = f.gets
        process.call line.force_encoding('utf-8')
      end
    end
  end

  def generate_tasks()
    @project_dependency_map.each do |from, to|
      to.each do |referenced|
        file @project_to_artifact_map[from]=> @project_to_artifact_map[referenced]
      end
    end
  end

  private

  def self.last_test_pass_note(assembly_path)
    "#{assembly_path}.pass"
  end

  def process_csproj(csproj_path)
    csproj_root = File.dirname csproj_path

    # metadata to collect
    assembly_name = nil
    output_type = nil
    assembly_path = nil
    source_paths = []
    resource_paths = []
    project_references = []
    depends_on_xunit = false

    in_debug = false
    File.open(csproj_path, 'r') do |fp|
      Xml.new
        .tag_end("Project/PropertyGroup/AssemblyName", lambda {|value| assembly_name = value })
        .tag_end("Project/PropertyGroup/OutputType", lambda {|value| output_type = value })
        .tag_start("Project/PropertyGroup", lambda {|attrs| in_debug = /Debug/ =~ attrs['Condition'] })
        .tag_end("Project/PropertyGroup/OutputPath", lambda {|value| assembly_path = normalize_path "#{csproj_root}/#{value}#{assembly_name}.#{if output_type == "Exe" then "exe" else "dll" end}" if in_debug })
        .tag_start("Project/ItemGroup/Compile", lambda {|attrs| source_paths.push(normalize_path "#{csproj_root}/#{attrs['Include']}") })
        .tag_start("Project/ItemGroup/ProjectReference", lambda {|attrs| project_references.push(normalize_path "#{csproj_root}/#{attrs['Include']}") })
        .tag_start("Project/ItemGroup/Reference", lambda {|attrs| depends_on_xunit = true if attrs['Include'].start_with? 'xunit.core,' })
        .tag_start("Project/ItemGroup/None", lambda {|attrs| resource_paths.push(normalize_path "#{csproj_root}/#{attrs['Include']}") })
        .parse fp
    end

    @project_dependency_map[csproj_path] = project_references
    @project_to_artifact_map[csproj_path] = assembly_path

    file csproj_path

    if depends_on_xunit
      last_test_pass_note = System.last_test_pass_note(assembly_path)
      task :test => last_test_pass_note
      file last_test_pass_note => assembly_path do
        if File.exist? assembly_path
          begin
            @env.xunit "#{assembly_path}"
            verbose(false) { touch last_test_pass_note }
          rescue => e
            ERRORS.push "Tests failed for #{assembly_path}: #{e}"
          end
        end
      end

      task :clean do
        rm_f last_test_pass_note
      end
    end

    file assembly_path do
      begin
        verbose(false) { rm_f assembly_path } # force the builder to work
        @env.builder.build_project(csproj_path)
	verbose(false) { touch assembly_path }
        puts "Built: #{assembly_path}"
      rescue => e
        ERRORS.push "Build failed for #{assembly_path}: #{e}"
      end
    end

    task :build => assembly_path

    task :clean do
      rm_f assembly_path
    end

    file assembly_path => csproj_path

    source_paths.each do |source_path|
      file source_path
      file assembly_path => source_path
    end

    resource_paths.each do |resource_path|
      file resource_path
      file assembly_path => resource_path
    end
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
          unless tag_buffer.end_with? "--"
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

    unless @stack.empty?
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

# Thanks to @mislav for this, found at https://stackoverflow.com/a/5471032
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
  include Rake::DSL

  def build_project(csproj_path)
    raise "Not implemented"
  end
end

class MSBuild < Build
  def initialize(env)
    @env = env
    @msbuild = normalize_path(which "msbuild")
    @msbuild = FileList.new(normalize_path "#{ENV['windir']}/Microsoft.NET/Framework/**/MSBuild.exe").last if @msbuild == nil
    raise "Can't find MSBuild" if @msbuild == nil
  end

  def build_project(csproj_path)
    @env.exec_quiet "\"#{@msbuild}\" /nologo /m:4 /v:quiet #{csproj_path}"
  end
end

class XBuild < Build
  def initialize(env)
    @env = env
    @xbuild = which "xbuild"
    raise "Can't find XBuild" if @xbuild == nil
  end

  def build_project(csproj_path)
    @env.exec_quiet "\"#{@xbuild}\" /nologo /v:quiet #{csproj_path}"
  end
end

class Env
  include Rake::DSL

  def self.construct()
    ENV['windir'] == nil ? Posix.new : Win.new
  end

  def nuget(cmd)
    raise "Not implemented"
  end

  def builder()
    raise "Not implemented"
  end

  def xunit(cmd)
    raise "Not implemented"
  end

  def exec_quiet(cmd)
    require 'open3'
    Open3.popen2e(cmd) do |stdin, stdout_and_stderr, wait_thr|
      throw "ERROR:\n#{stdout_and_stderr.gets}" if wait_thr.value.exitstatus != 0
    end
  end
end

class Win < Env
  def builder()
    MSBuild.new self
  end

  def nuget(cmd)
    nuget = FileList.new("**/nuget.exe").last
    nuget = which "nuget" if nuget == nil

    if nuget == nil
      require 'net/http'
      begin
        puts "Downloading nuget..."
        Net::HTTP.start("dist.nuget.org") do |http|
            resp = http.get("/win-x86-commandline/latest/nuget.exe")
            open("nuget.exe", "wb") do |file|
                file.write(resp.body)
            end
        end
        puts "...done!"
        nuget = "./nuget.exe"
      rescue => e
        raise "ERROR: download of nuget failed, please install manually\n#{e}"
      end
    end

    exec_quiet "\"#{nuget}\" #{cmd}"
  end

  def xunit(cmd)
    @xunit = FileList.new("**/xunit.console.exe").last if @xunit == nil
    verbose(false) { sh "#{@xunit} #{cmd}" }
  end
end

class Posix < Env
  def builder()
    XBuild.new self
  end

  def nuget(cmd)
    nuget = which "nuget"
    raise "Please install nuget" if nuget == nil
    exec_quiet "\"#{nuget}\" #{cmd}"
  end

  def nuget(cmd)
    nuget = FileList.new("**/nuget.exe").last
    if nuget != nil
      exec_quiet "mono \"#{nuget}\" #{cmd}"
    else
      nuget = which "nuget"
      raise "Please install nuget" if nuget == nil
      exec_quiet "\"#{nuget}\" #{cmd}"
    end
  end

  def xunit(cmd)
    @xunit = FileList.new("**/xunit.console.exe").last if @xunit == nil
    # using -noshadow per https://github.com/xunit/xunit/issues/957
    verbose(false) { sh "mono #{@xunit} #{cmd} -noshadow | tr -d '\\f'; exit $PIPESTATUS" }
  end
end


### Initialization
system = System.new Env.construct
FileList.new("**/*.sln").each do |sln_path|
  system.process_sln(sln_path)
end
system.generate_tasks
