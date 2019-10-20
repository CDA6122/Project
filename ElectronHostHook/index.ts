/**
 * Authors: David Bruck (dbruck1@@fau.edu) and Freguens Mildort (fmildort2015@@fau.edu)
 * Original source: https://github.com/CDA6122/Project
 * License: BSD 2-Clause License (https://opensource.org/licenses/BSD-2-Clause)
 **/
// @ts-ignore
import * as Electron from "electron";
import { Connector } from "./connector";

export class HookService extends Connector {
    constructor(socket: SocketIO.Socket, public app: Electron.App) {
        super(socket, app);
    }

    onHostReady(): void {
        this.on("handle-new-window", async(done) => {
            for (const webContents of Electron.webContents.getAllWebContents()) {
                webContents.on("new-window", (event, url) => {
                    event.preventDefault();
                    Electron.shell.openExternal(url);
                });
            }
            done();
        })
    }
}

