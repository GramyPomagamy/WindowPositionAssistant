# Window Position Assistant

A small agent that provides information about windows on the screen.


## Installation

Requirements:
- [.NET Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-6.0.8-windows-x64-installer)
- [ASP.NET Core Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-aspnetcore-6.0.8-windows-x64-installer)

The latest stable version of the application itself can be found in the [Releases](https://github.com/GramyPomagamy/WindowPositionAssistant/releases) page.
Development builds are available as downloadable artifacts in [Github Actions](https://github.com/GramyPomagamy/WindowPositionAssistant/actions/).


## Usage

Just start the `WindowPositionAssistant.exe` program.
It will run in the background and show the <img src="WindowPositionAssistant/icon.ico" height="20px"> icon in the taskbar.

You can use the icon to close the program or enable `Active Sending Mode`, which will push window list to the remote proxy server periodically instead of only passively waiting for connections.
When `Active Sending Mode` is enabled, you can check the remote `ID` number by clicking on the program's icon.
Clicking the option again will disable sending.

By default the application listens on all interfaces on port `55338`.
It exposes a single endpoint, `/windows`, that returns an array of windows that meets the following [JSON Schema](https://json-schema.org/) spec:
```json
{
    "$schema": "http://json-schema.org/draft-04/schema#",
    "type": "object",
    "additionalProperties": false,
    "properties": {
        "pid": {
            "type": "number"
        },
        "processName": {
            "type": "string"
        },
        "windowTitle": {
            "type": "string"
        },
        "x": {
            "type": "number"
        },
        "y": {
            "type": "number"
        },
        "w": {
            "type": "number"
        },
        "h": {
            "type": "number"
        }
    },
    "required": [
        "pid",
        "processName",
        "windowTitle",
        "x",
        "y",
        "w",
        "h"
    ]
}
```


## Configuration

Application can be configured using the `appsettings.json` file.

The most important settings are:
- `Kestrel.Endpoints.Http.Url` sets the address and port to bind a local server to.
- `ProxySettings.Url` sets the remote proxy server to use when `Active Sending Mode` is enabled.
  You can host your own proxy server using [Window Position Assistant Proxy](https://github.com/GramyPomagamy/WindowPositionAssistantProxy).
- `ProxySettings.PeriodMs` sets the interval between remote server updates in `Active Sending Mode`.
