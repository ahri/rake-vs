# # gem install guard guard-shell
# $ guard -ci

require 'open3'
ignore %r{/\.}

guard :shell do
  watch /^(Rakefile|.*\.(cs|csproj|sln|csv|xml|json|ya?ml))$/ do |m|
    puts ">>> #{m[0]} changed"

    Open3.popen2e "rake" do |stdin, stdout_and_stderr, wait_thr|
      stdout_and_stderr.each { |line| print line }
      puts ">>> Rake completed"
    end
  end
end
