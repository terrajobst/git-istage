# git istage

[![Build Status](https://terrajobst.visualstudio.com/git-istage/_apis/build/status/terrajobst.git-istage?branchName=master)](https://terrajobst.visualstudio.com/git-istage/_build/latest?definitionId=14)

This git extension is designed to be a better alternative to `git add -p`.
The goal is to make staging whole files, as well as parts of a file, up to
the line level, a breeze. See [documentation](docs/about.md) for details.

![](docs/screen.png)

## Installation

    $ dotnet tool install git-istage -g

### CI Builds

    $ dotnet tool install git-istage -g --add-source https://www.myget.org/F/git-istage-ci/api/v3/index.json

## Documentation

See [documentation](docs/about.md) for details.

## Missing features

* Support inline search using slash
* Add ability to edit the patch inline
* Add help page with shortcut overview
