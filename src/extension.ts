import * as vscode from 'vscode';
import * as editing from './editing';
import * as path from 'path';
import * as lspClient from './lsp-client';
import { ClientRequest } from 'http';


// this method is called when your extension is activated
// your extension is activated the very first time the command is executed
export function activate(context: vscode.ExtensionContext)
{
	console.log("dkcode extension activating");

	// Register commands
	context.subscriptions.push(vscode.commands.registerTextEditorCommand('dkcode.addFileHeader', editing.addFileHeader));
	context.subscriptions.push(vscode.commands.registerTextEditorCommand('dkcode.insertDate', editing.insertDate));

	// // Initialize the language server
	// lspServer.initializeLspServer();

	// Initialize the language client
	lspClient.setServerModule(context.asAbsolutePath(path.join('language_server', 'bin', 'netcoreapp3.1', 'DK.LanguageServer.exe')));
	vscode.workspace.onDidOpenTextDocument(lspClient.didOpenTextDocument);
	vscode.workspace.textDocuments.forEach(lspClient.didOpenTextDocument);

	// let disposable = vscode.languages.registerHoverProvider('dk', {
	// 	provideHover(document, position, token) {

	// 		console.log("hover at line " + position.line + ", character ", position.character);
	// 		let wordRange = document.getWordRangeAtPosition(position);
	// 		let word = document.getText(wordRange);
	// 		console.log("word: " + word);

	// 		return new vscode.Hover(word);
	// 	}
	// });
	// context.subscriptions.push(disposable);
}

// this method is called when your extension is deactivated
export function deactivate() : Thenable<void> | undefined
{
	console.log("dkcode extension deactivating");

	return lspClient.stop();
}
