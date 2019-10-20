# CDA6122 Fall 2019 Project

Authors: David Bruck (dbruck1@fau.edu) and Freguens Mildort (fmildort2015@fau.edu)  
Original source: https://github.com/CDA6122/Project  
License: BSD 2-Clause License (https://opensource.org/licenses/BSD-2-Clause)



## Purpose

This simulation project explores cross-layer optimizations in wireless ad-hoc networks for improving quality of service (QoS) in content distribution. By storing multiple copies of the same content distributed amongst an array of interconnected nodes, we can analyze the effects of storing additional copies of the same content and the available wireless bandwidth between nodes on the QoS. We will use continuous random variables across multiple simulation runs, some uniform random like when a nodes initially request content, and others Gaussian random like the likelihood particular content will be requested (popularity) and the file size of the content. QoS will be measured by the average amount of time between when a node makes the initial request for content and when enough of the content has buffered so the remainder could be streamed without stopping.



## Table of Contents

1. [Quick Run](#quick-run)
2. [Download, Compile, and Run](#download-compile-and-run)
   - [Prerequisites](#prerequisites)
   - [Verify Prerequisites](#verify-prerequisites)
   - [First time instructions](#first-time-instructions)
   - [Then, to run it the first time and every time afterwards](#then-to-run-it-the-first-time-and-every-time-afterwards)
3. [Build for Release](#build-for-release)
4. [How we did it](#how-we-did-it)
5. [References](#references)



## Quick Run

Follow these instructions if you just want to run the pre-compiled releases.

### Releases

In the Releases tab on the GitHub CDA6122 Project page for each release version, you will see a set of files by different Operating System. The ‘packed’ file is sufficient to run the application, but all files produced by `electronize build` will be provided in addition. The ‘blockmap’ file is a gzipped JSON file listing the application files with indexes and content hashes. ‘latest.yml’ file describes basic information about the release including the version, the files, the release date, and content hashes. The ‘unpacked’ file is a zip file was created by packing the unpacked output folder.
**You only need to download the packed file to run the application.*

* Windows[^windows]
  * `Project Setup [version].exe` **packed application, for installation*
  * `Project Setup [version].exe.blockmap`
  * `latest.yml`
  * `win-unpacked.zip`
* OSX[^macos]
  * `Project-[version].dmg` **packed application, for installation*
  * `Project-0.0.1.dmg.blockmap`
  * `latest-mac.yml`
  * `Project.app` **packed application*
  * `Project-[version]-mac.zip`
* Linux[^linux]
  * `Project-[version].AppImage` **packed application*
  * `electron.net.host_[version]_amd64.snap`
  * `latest-linux.yml`
  * `linux-unpacked.tar.gz`



## Download, Compile, and Run

Follow these instructions if you want to view the project source, or make changes and test them.

### Prerequisites

1. Install git for your current Operating System.
   *During installation, if prompted, choose to add its path to your environment with option like “`Git from the command line and also from 3rd-party software`”*
   https://git-scm.com/downloads
2. Install .NET Core 2.2 SDK for your current Operating System from:
   https://dotnet.microsoft.com/download/dotnet-core/2.2
3. Install .NET Core 3.0 SDK for your current Operating System from:
   https://dotnet.microsoft.com/download/dotnet-core/3.0
4. Install Node.js (can use LTS version):
   https://nodejs.org/en/

### Verify Prerequisites

Open a new terminal and ensure the following executables can be found (can use ‘`where`’ on Windows or ‘`whereis`’ on Linux):
`git` (e.g. “`where git`” on Windows)
`npm` (e.g. “`whereis dotnet`” on Linux)

In the terminal, run the following command and ensure it lists at least the two lines starting 3.0.X and 2.2.X (X can be anything):
`dotnet --list-sdks`

In the terminal, change directory to the desired parent directory which will contain the Project subdirectory (containing the Project source code).
Follow instructions from the GitHub CDA6122 Project page to Clone or download. For example, you can use the following terminal command:
`git clone https://github.com/CDA6122/Project.git`

### First time instructions

In the terminal, change directory to the Project subfolder.
Run the following commands (omit the “sudo” command prefix if running on Windows; also, installing electron-builder seems to take a very long time so be patient):
`dotnet tool install ElectronNET.CLI -g`
`sudo npm install electron-builder --global`

**Note:** *You may have to restart the computer if terminal says `electronize` command not found when you try to run it*

### Then, to run it the first time and every time afterwards

In the terminal, change directory to the Project subfolder
Run the following command:
`electronize start` 



## Build for Release

Follow these instructions if you want to build a release such as for the [Quick Run](#quick-run).
*First, follow the instructions for [Download, Compile, and Run](#download-compile-and-run), if you haven't already*

First, delete all files in the build output directory like: [source_root]/bin/Desktop/*

Open a new terminal, change directory to the Project subfolder, and run only the one following command appropriate for the desired Operating System target (the first time running the command may take a while so be patient):

1. `electronize build /target win` *build for Windows is supported cross-platform, but requires wine to be installed*[^wine]
2. `electronize build /target osx` *build for macOS is supported only on macOS, please see https://electron.build/multi-platform-build**

**Note:** *Building on macOS may give some errors. Provided are some common errors and resolutions. After attempting each resolution, try the build again:*

- Error: ‘`electron-builder update check failed`’
  Resolution: follow the instructions to ‘`get access to the local update config store`’
- Error (if the exact path to directory `/Users/davidbruck/.npm/_cacache/index-v5` differs in the error message, change it in the resolution command as well):
```shell
  ⨯ npm exited with code ERR_ELECTRON_BUILDER_CANNOT_EXECUTE
Error output:
Error output:
Unhandled rejection Error: EACCES: permission denied, mkdir '/Users/davidbruck/.npm/_cacache/index-v5/f9/55'
```
​       Resolution:
```shell
sudo chmod -R g+w /Users/davidbruck/.npm/_cacache/index-v5
```

3. `electronize build /target linux` *build for Linux is supported cross-platform (i.e. from Windows as well), but requires the free https://service.electron.build/find-build-agent to be available which, at the time of this writing, was not*

**Note:** *Building on Linux may give some errors. Provided are some common errors and resolutions. After attempting each resolution, try the build again:*

- Error: ‘`electron-builder update check failed`’
  Resolution: follow the instructions to ‘`get access to the local update config store`’
- Error (if the exact path to directory `/usr/lib/node_modules/electron-builder/node_modules/app-builder-lib/templates/icons` differs in the error message, change it in the resolution command as well):
```shell
  ⨯ chmod /usr/lib/node_modules/electron-builder/node_modules/app-builder-lib/templates/icons/electron-linux/256x256.png: operation not permitted
```
​       Resolution:
```shell
sudo find /usr/lib/node_modules/electron-builder/node_modules/app-builder-lib/templates/icons -type d -user root -exec sudo chown -R $USER: {} +
```


## How we did it

1. First, we picked HTML/CSS for the presentation layer.[^htmlcss]
2. Second, we picked C# to drive changes to the presentation layer (instead of JavaScript) and to perform the simulation itself. (C#)[^ecma334] (JavaScript)[^ecma262]
3. Tools (all cross-platform):
   1. Electron was used as a way to bundle Chromium for rendering the HTML/CSS. It runs as a Desktop application.[^electron] [^chromium]
   2. Normally, Electron has a JavaScript API for controlling application logic, but we used Electron.NET so we can use .NET Core to serve an in-process web application instead of Node.js or others.[^electronnet]
   3. Electron uses Node.js as an additional runtime and Electron.NET also uses npm packages such as for ElectronHostHook.[^nodejs]
   4. Electron.NET works on .NET Core 2.2, but we are using it to host a web application on .NET Core 3.0 (hence two different versions in the required dependencies).[^dotnet]
   5. .NET Core 3.0 includes support for Blazor Server which allows us to drive changes to the presentation layer in C# instead of JavaScript.[^blazor]
4. The pre-compiled build outputs are also cross-platform, but required all platforms to create them.



## References

[^windows]:  http://microsoft.com/windows  
[^macos]: https://www.apple.com/macos  
[^linux]: https://www.kernel.org/  
[^wine]:  https://www.winehq.org/  
[^htmlcss]:  https://www.w3.org/standards/webdesign/htmlcss.html  
[^ecma334]:  https://www.ecma-international.org/publications/standards/Ecma-334.htm  
[^ecma262]:  https://www.ecma-international.org/publications/standards/Ecma-262.htm  
[^electron]: https://electronjs.org/  
[^chromium]: https://www.chromium.org/  
[^electronnet]: https://github.com/ElectronNET/Electron.NET  
[^nodejs]: https://nodejs.org/  
[^dotnet]: https://dotnet.microsoft.com/  
[^blazor]: https://docs.microsoft.com/en-us/aspnet/core/blazor
