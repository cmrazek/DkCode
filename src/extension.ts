// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
import * as vscode from 'vscode';
import * as editing from './editing';

// this method is called when your extension is activated
// your extension is activated the very first time the command is executed
export function activate(context: vscode.ExtensionContext)
{
	context.subscriptions.push(vscode.commands.registerTextEditorCommand('dkcode.addFileHeader', editing.addFileHeader));

	let disposable = vscode.languages.registerHoverProvider('dk', {
		provideHover(document, position, token) {

			console.log("hover at line " + position.line + ", character ", position.character);
			let wordRange = document.getWordRangeAtPosition(position);
			let word = document.getText(wordRange);
			console.log("word: " + word);

			return new vscode.Hover(word);
		}
	});
	context.subscriptions.push(disposable);
}

// this method is called when your extension is deactivated
export function deactivate() {}
