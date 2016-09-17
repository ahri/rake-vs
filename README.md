Rake for Visual Studio Solutions/Projects
=========================================
> Build and test your Visual Studio solution using [Rake](http://rake.rubyforge.org/)


The problem
-----------
Getting started with building solutions with Visual Studio is tedious,
especially if you're having to contend with build servers, multi-platform, etc.

In addition, the default VS test runner is possibly the slowest thing in the
known universe. ReSharper is a lot better but it both costs money, and isn't as
intelligent about running tests as it could be (and has weird caching behaviour
when you change the names of your classes/methods).

The clincher, though, is how unreliable MSBuild is at detecting when it needs to
rebuild artifacts, leading to stale DLLs yielding odd results on occaision -
this unreliability is just terrible and unforgivable.


The solution (get it?!)
-----------------------
For the cost of a dependency on Ruby (with comes bundled with Rake on Windows)
you can have a speedy, cross-platform build system that only builds when
neccessary, only runs tests when needed - but *always* runs when needed.

Not only this, but you also get to tweak the dependencies of your build to the
nth degree with full access to Rake and a complete language to express yourself
in - need to download a random number from an online source and package your
artifacts up into a zip with that filename? Rake's got you covered.


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
rake push     # Push to git after testing
rake test     # Run tests, as needed

$ rake clean test    # remove binaries and test everything
$ rake clean build   # remove binaries and build everything, but don't test
```

Laziness
--------
These examples pertain to the [sample solution](https://github.com/ahri/rake-vs/tree/sample).

```sh
$ git clean -xfd  # remove everything we haven't committed to source control
$ rake
Downloading nuget...
...done!
Built: Sample/Library/bin/Debug/Library.dll
Built: Sample/Sample/bin/Debug/Sample.exe
Built: Sample/Tests/bin/Debug/Tests.dll
xUnit.net Console Runner (64-bit .NET 4.0.30319.42000)
  Discovering: Tests
  Discovered:  Tests
  Starting:    Tests
  Finished:    Tests
=== TEST EXECUTION SUMMARY ===
   Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Time: 0.141s

$ rake # does nothing - we've not changed any files

$ touch Sample/Sample/Program.cs # pretend we made a change to Program.cs
$ rake
Built: Sample/Sample/bin/Debug/Sample.exe
# Note: the exe was rebuilt, but no tests were run

$ touch Sample/Library/Library.cs # change something lower down
Built: Sample/Library/bin/Debug/Library.dll
Built: Sample/Sample/bin/Debug/Sample.exe
Built: Sample/Tests/bin/Debug/Tests.dll
xUnit.net Console Runner (64-bit .NET 4.0.30319.42000)
  Discovering: Tests
  Discovered:  Tests
  Starting:    Tests
  Finished:    Tests
=== TEST EXECUTION SUMMARY ===
   Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Time: 0.121s
# Note that a rebuild of everything depending on Library.dll was triggered,
# which in turn forced re-execution of affected tests

$ touch Sample/Sample/Resources/icon.ico # make a change to an icon the exe depends on
$ rake
Built: Sample/Sample/bin/Debug/Sample.exe # only rebuild the exe
```

Extra features!
---------------
### Has a task to push to git
And of course it only pushes if your tests are passing. And it only tests what
hasn't already been tested. Awesome.

### Works on Linux and OSX, maybe even BSD!
Through the magic of Mono and XBuild, the Rakefile can build in non-Windows
environments, making it the easiest cross-platform .NET build tool abstraction
in the universe. Guaranteed!

### TeamCity compatible
Right now this is only because XUnit supports TeamCity natively ;)

```sh
$ TEAMCITY_PROJECT_NAME=test rake clean test
rm -f Sample/Sample/bin/Debug/Sample.exe
rm -f Sample/Library/bin/Debug/Library.dll
rm -f Sample/Tests/bin/Debug/Tests.dll.pass
rm -f Sample/Tests/bin/Debug/Tests.dll
Built: Sample/Library/bin/Debug/Library.dll
Built: Sample/Sample/bin/Debug/Sample.exe
Built: Sample/Tests/bin/Debug/Tests.dll
xUnit.net Console Runner (64-bit .NET 4.0.30319.42000)
##teamcity[testSuiteStarted name='Test collection for Tests.Tests (1)' flowId='231651e4c84b444f8090dbe307b5a75c']
##teamcity[testStarted name='Tests.Tests.Test' flowId='231651e4c84b444f8090dbe307b5a75c']
##teamcity[testFinished name='Tests.Tests.Test' duration='31' flowId='231651e4c84b444f8090dbe307b5a75c']
##teamcity[testSuiteFinished name='Test collection for Tests.Tests (1)' flowId='231651e4c84b444f8090dbe307b5a75c']
```

TODO
----
- [ ] Colours & unicode?
- [ ] MSTest on Windows?
- [ ] NUnit on Windows?
- [ ] NUnit on Posix?
