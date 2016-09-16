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
- [ ] XBuild
- [ ] XUnit on Posix
- [ ] XUnit on Windows
