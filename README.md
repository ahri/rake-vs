Rake for Visual Studio Solutions/Projects
=========================================
> Build and test your Visual Studio solution using Rake


**_Work-in-progress_**


Usage
-----

Copy the Rakefile to your solution dir (or above it), install Ruby either via
your package manager or on Windows the
[RubyInstaller](http://rubyinstaller.org/) works nicely. Then, on the
command-line, execute as follows:

```sh
$ rake -T
rake build    # Build everything, as needed
rake clean    # Remove all assemblies and test notes
rake default  # Build and test everything, as needed
rake test     # Run tests, as needed

$ rake clean test    # remove binaries and test everything
$ rake clean build   # remove binaries and build everything, but don't test
```


Status
------

- [x] MSBuild
- [x] Sample project (in a branch)
- [x] nuget restore
- [ ] demo nuget restore
- [ ] Resource-awareness
- [ ] demo resource awareness
- [x] XUnit on Windows
- [ ] demo only execute tests when needed
- [ ] Display in terms of artifacts
- [x] TeamCity support
- [ ] demo xunit output when TEAMCITY_PROJECT_NAME is set
- [ ] XBuild
- [ ] XUnit on Posix
- [ ] Colours & unicode?
- [ ] MSTest on Windows?
- [ ] NUnit on Windows?
- [ ] NUnit on Posix?
