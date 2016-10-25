# # gem install guard guard-shell
# $ guard -ci

require 'pty'
ignore %r{/\.}

guard :shell do
  watch /^(Rakefile|.*\.(cs|csproj|sln|csv|xml|json|ya?ml))$/ do |m|
    puts "#{m[0]} changed"

    begin
      PTY.spawn "rake" do |stdout, stdin, pid|
        begin
          stdout.each { |line| print line }
        rescue Errno::EIO
        end

        puts ">>> Rake completed"
      end
    rescue PTY::ChildExited
      puts ">>> Rake failed"
    end
  end
end
