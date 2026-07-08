# Development

This document describes how to build and run this project on your local machine.

## Getting started

The begin you'll need to have installed [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download)
and [Git](https://git-scm.com/) or one of the graphical Git clients out there.

1. Clone this repository to your local machine and navigate into the project
   folder.
2. Create a copy of the file "Directory.Build.props.template" and remove the
   ".template" from the name so the copied file is called
   `Directory.Build.props`.
3. Open the `Directory.Build.props` file and change the Resonite install path if
   required. The path needs to end with a slash otherwise you'll get build
   errors.
4. Finally, run `dotnet build -c Release` in the project folder to compile the
   mod. If everything worked you should see a `Build succeeded`.

After building the mod will automatically copy itself into your Resonite
`rml_mods` folder so that you're now running your current build.

### Debugging and hot reload

This mod supports the [ResoniteHotReloadLib](https://github.com/Nytra/ResoniteHotReloadLib)
by Nytra when compiling in Debug mode. To make use of this you'll need to make
sure you follow the instructions in the Pre-requisites section for that mod
first. After that you can compile the mod in Debug mode and it will
automatically copy itself to the correct folder and allow you to reload the mod
from inside the game in accordance with the Usage section for that mod.
