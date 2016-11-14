task :default => :test

desc "Run tests, as needed, this is the default action!"
task :test => :build

desc "Build everything, as needed"
task :build => [:build_Debug, :build_Release]

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

require './CustomRake' if File.exist? './CustomRake.rb'

### General tooling

require 'open3'

STDOUT.sync = true
ERRORS = []
at_exit do
  exit 0 if ERRORS.empty? and $!.nil?

  ERRORS.each { |err| STDERR.puts err }
  exit 1
end

def exec_quietly(cmd)
  exec_with_exclusions(cmd, [/.*/])
end

def exec_with_exclusions(cmd, exclusions)
  Open3.popen2e(cmd) do |stdin, stdout_and_stderr, wait_thr|
    while line = stdout_and_stderr.gets
      exclude = false

      if wait_thr.value.exitstatus == 0
        exclusions.each do |exclusion|
          if exclusion =~ line
            exclude = true
            break
          end
        end
      end

      puts line unless exclude
    end

    throw "ERROR: cmd failed with #{wait_thr.value.exitstatus}: #{cmd}" if wait_thr.value.exitstatus != 0
  end
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

    unless FileList.new("**/packages.config").empty?
      @env.nuget "restore #{ENV['NUGET_SOURCE'] and "-Source #{ENV['NUGET_SOURCE']} "}#{sln_path}"
    end

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
        @project_to_artifact_map[from].each do |cfg, artifact|
          next if not @project_to_artifact_map[referenced].key? cfg
          file artifact => @project_to_artifact_map[referenced][cfg]
        end
      end
    end
  end

  private

  def self.last_test_pass_note(assembly_path)
    "#{assembly_path}.pass"
  end

  def process_csproj(csproj_path)
    csproj_root = File.dirname csproj_path
    file csproj_path

    # metadata to collect
    assembly_name = nil
    output_type = nil
    source_paths = []
    resource_paths = []
    project_references = []
    depends_on_xunit = false

    configuration_paths = {}
    build_cfg = nil
    File.open(csproj_path, 'r') do |fp|
      Xml.new
        .tag_end("Project/PropertyGroup/AssemblyName", lambda {|value| assembly_name = value })
        .tag_end("Project/PropertyGroup/OutputType", lambda {|value| output_type = value })
        .tag_start("Project/PropertyGroup", lambda {|attrs| /== '(?<build_cfg>[^'|]+)\|/ =~ attrs['Condition'] })
        .tag_end("Project/PropertyGroup/OutputPath", lambda {|value| configuration_paths[build_cfg] = normalize_path "#{csproj_root}/#{value}#{assembly_name}.#{if output_type == "Exe" then "exe" else "dll" end}" })
        .tag_start("Project/ItemGroup/Compile", lambda {|attrs| source_paths.push(normalize_path "#{csproj_root}/#{attrs['Include']}") })
        .tag_start("Project/ItemGroup/ProjectReference", lambda {|attrs| project_references.push(normalize_path "#{csproj_root}/#{attrs['Include']}") })
        .tag_start("Project/ItemGroup/Reference", lambda {|attrs| depends_on_xunit = true if attrs['Include'].start_with? 'xunit.core,' })
        .tag_start("Project/ItemGroup/None", lambda {|attrs| resource_paths.push(normalize_path "#{csproj_root}/#{attrs['Include']}") })
        .parse fp
    end

    @project_dependency_map[csproj_path] = project_references
    configuration_paths.each do |cfg, assembly_path|
      if depends_on_xunit
        next if cfg != "Debug"

        last_test_pass_note = System.last_test_pass_note(assembly_path)
        task :test => last_test_pass_note
        file last_test_pass_note => assembly_path do
          if File.exist? assembly_path
            begin
              print "Testing #{assembly_path}... "
              @env.xunit "#{assembly_path}"
              puts "passed"
              verbose(false) { touch last_test_pass_note }
            rescue => e
              ERRORS.push "Tests failed for #{assembly_path}: #{e}"
              puts
            end
          end
        end

        task :clean do
          rm_f last_test_pass_note
        end
      end

      @project_to_artifact_map[csproj_path] = {} if not @project_to_artifact_map.key? csproj_path
      @project_to_artifact_map[csproj_path][cfg] = assembly_path

      file assembly_path do
        begin
          verbose(false) { rm_f File.dirname(assembly_path) } # force the builder to work
          verbose(false) { parts = assembly_path.rpartition("/bin/"); rm_rf File.dirname("#{parts[0]}/obj/#{parts[2]}") } # force the builder to work
          @env.builder.build_project(csproj_path, cfg)
          verbose(false) { touch assembly_path }
          puts "Built: #{assembly_path}"
        rescue => e
          ERRORS.push "Build failed for #{assembly_path}: #{e}"
        end
      end

      task "build_#{cfg}" => assembly_path

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

  def build_project(csproj_path, cfg)
    raise "Not implemented"
  end
end

class MSBuild < Build
  def initialize(env)
    @env = env
    @msbuild = normalize_path(which "msbuild")
    @msbuild = FileList.new(normalize_path "#{ENV['WINDIR']}/Microsoft.NET/Framework/**/MSBuild.exe").last if @msbuild == nil
    raise "Can't find MSBuild" if @msbuild == nil
  end

  def build_project(csproj_path, cfg)
    exec_quietly "\"#{@msbuild}\" /nologo /m:4 /v:quiet /property:Configuration=#{cfg} #{csproj_path}"
  end
end

class XBuild < Build
  def initialize(env)
    @env = env
    @xbuild = which "xbuild"
    raise "Can't find XBuild" if @xbuild == nil
  end

  def build_project(csproj_path, cfg)
    exec_quietly "\"#{@xbuild}\" /nologo /v:quiet /property:Configuration=#{cfg} #{csproj_path}"
  end
end

class Env
  include Rake::DSL

  def self.construct()
    ENV['WINDIR'] == nil ? Posix.new : Win.new
  end

  def exec_dotnet(cmd)
    raise "Not implemented"
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

  def self.nuget_download()
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
      return "./nuget.exe"
    rescue => e
      raise "ERROR: download of nuget failed, please install manually\n#{e}"
    end
  end

  def self.nuget_path()
    nuget = FileList.new("**/nuget.exe").map {|path| "./#{path}"}.last
    nuget = which "nuget" if nuget == nil
    nuget = Env.nuget_download if nuget == nil
    raise "Please install nuget" if nuget == nil

    return nuget
  end

  def self.nuget_exclusions()
    [/^MSBuild auto-detection: /, /^All packages .* are already installed\.$/]
  end
end

class Win < Env
  def builder()
    MSBuild.new self
  end

  def exec_dotnet(cmd)
    exec_quietly cmd
  end

  def nuget(cmd)
    exec_with_exclusions("\"#{Env.nuget_path}\" #{cmd}", Env.nuget_exclusions)
  end

  def xunit(cmd)
    @xunit = FileList.new("**/xunit.console.exe").last if @xunit == nil
    exec_quietly "#{@xunit} #{cmd} #{ENV['XUNIT_OPTIONS']}"
  end
end

class Posix < Env
  def builder()
    XBuild.new self
  end

  def exec_dotnet(cmd)
    exec_quietly "mono #{cmd}"
  end

  def nuget(cmd)
    path = Env.nuget_path
    exec_with_exclusions("#{path.end_with?(".exe") ? "mono " : ""}\"#{path}\" #{cmd}", Env.nuget_exclusions)
  end

  def xunit(cmd)
    @xunit = FileList.new("**/xunit.console.exe").last if @xunit == nil
    # using -noshadow per https://github.com/xunit/xunit/issues/957
    exec_quietly "bash -c 'mono #{@xunit} #{cmd} #{ENV['XUNIT_OPTIONS']} -noshadow | tr -d '\\f'; exit $PIPESTATUS'"
  end
end


### Initialization
ENVIRONMENT = Env.construct
system = System.new ENVIRONMENT
FileList.new("**/*.sln").each do |sln_path|
  system.process_sln(sln_path)
end
system.generate_tasks
