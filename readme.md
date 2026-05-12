# WebShell

This is a quick thing I made which allows you to turn a website hosted on localhost into an all in one "app"/webapp. You configure it to start the web server/whatever and when you open the app it will start the server, then open a window which connects to it. When the app is closed, the server is closed. I mainly wanted this for the OpenCode web ui which allows you to host a server.

## How to use
To get started, download and extract the .zip, (Windows only, ARM64 and x86_64) to wherever you want your app. Open `config.json` and you will see this:
```json
{
    "customTitle": "WebShell",
    "useDocumentTitle": false,
    "server": {
        "port": 1234,
        "path": "path-to-executable",
        "arguments": ""
    }
}
```

Should mostly be self explanatory, but here is what I use for OpenCode:
```json
{
    "customTitle": "OpenCode",
    "useDocumentTitle": false,
    "server": {
        "port": 4096,
        "path": "C:\\Users\\<name>\\AppData\\Roaming\\npm\\node_modules\\opencode-ai\\node_modules\\opencode-windows-arm64\\bin\\opencode.exe",
        "arguments": "serve --port 4096 --hostname 127.0.0.1"
    }
}
```
This starts OpenCode with the `serve` argument, which opens a server on port `4096` (on localhost), `localhost:4096` then serves the OpenCode web UI which is wrapped in a Avalonia/WebView window.

## Key things
- Open the app with WebShell.exe, you can rename this to whatever you want.
- After you close the app for the first time, the IconChanger executable will change the application icon to the favicon the server provides in the HTML document.
- The server is terminated as soon as the window closes, however if it crashes then the server will still be open, make sure you close it manually if the app does crash. It shouldn't really ever crash since it's simply a WebView wrapper but it might still be able to.
