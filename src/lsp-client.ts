import * as vscode from 'vscode';
import {
    LanguageClient,
    LanguageClientOptions,
    ServerOptions,
    TransportKind
} from 'vscode-languageClient';
//import { ChildProcess, exec } from 'child_process';

let g_client: LanguageClient;
let g_serverModule: string;

export function setServerModule(serverModule: string)
{
    g_serverModule = serverModule;
}

export function stop(): Thenable<void> | undefined
{
    if (g_client)
    {
        return g_client.stop();
    }
}

export function didOpenTextDocument(doc: vscode.TextDocument)
{
    if ((doc.languageId !== 'dk' && doc.languageId !== 'dkdict') || doc.uri.scheme !== 'file') { return; }

    console.log("Text document opened: " + doc.uri.toString());

    let folder = vscode.workspace.getWorkspaceFolder(doc.uri);
    if (!folder) { return; }

    if (!g_client)
    {
        console.log("Creating language client");

        let outputChannel: vscode.OutputChannel = vscode.window.createOutputChannel('DKCode');

        // let serverOptions: ServerOptions = {
        //     command: g_serverModule
        // };

        //let processRunner = () => { return new Promise<ChildProcess>(() => { return exec(g_serverModule); }); };

        let serverOptions: ServerOptions = {
            command: g_serverModule
        };

        let clientOptions: LanguageClientOptions = {
            documentSelector: [
                { scheme: 'file', language: 'dk' },
                { scheme: 'file', language: 'dkdict' }
            ],
            diagnosticCollectionName: 'dkcode',
            workspaceFolder: folder,
            outputChannel: outputChannel
        };

        g_client = new LanguageClient("dkcode", "DKCode LSP", serverOptions, clientOptions);
        g_client.start();
    }
}
