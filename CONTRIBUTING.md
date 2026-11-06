# Contributing

This is a personal project, built for one owner's daily use. Issues and ideas are welcome. Please open an issue before you start work on a change, because the owner may not accept a change that does not fit the spec.

## Before you change code

1. Read `docs/spec.md`. It holds the requirements.
2. Writing comes first. When writing conflicts with any other feature, writing wins.

## Build and run

The app needs Windows, the .NET SDK and a precision touchpad.

```bash
dotnet build
```

```bash
dotnet run --project src/TrackpadWhiteboard.App
```

## Pull requests

* Make one change in each pull request.
* Warnings are errors, so the build must have no warnings.
* Describe what you changed and how you checked it.

By contributing, you agree that your work is under the MIT License in `LICENSE`.
