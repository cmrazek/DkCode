// import {
// 	createConnection,
// 	TextDocuments,
// 	Diagnostic,
// 	DiagnosticSeverity,
// 	ProposedFeatures,
// 	InitializeParams,
// 	InitializeResult,
// 	DidChangeConfigurationNotification,
// 	CompletionItem,
// 	CompletionItemKind,
// 	TextDocumentPositionParams,
// 	TextDocumentSyncKind,
// 	Connection,
// 	IPCMessageReader
// } from 'vscode-languageserver';
// import {
// 	TextDocument,
// } from 'vscode-languageserver-textdocument';
// import * as fs from 'fs';

// let g_connection: Connection;

// let g_documents = new TextDocuments(TextDocument);

// //let g_hasConfigCap: boolean = false;
// let g_hasWorkspaceFolderCap: boolean = false;
// let g_hasDiagnosticRelationInfoCap: boolean = false;

// export function initializeLspServer()
// {
// 	g_connection = createConnection(factories: ProposedFeatures.all, input: new IPCMessageReader(process));

// 	g_connection.onInitialize((params: InitializeParams) =>
// 	{
// 		g_connection.console.log("g_connection.onInitialize (connection.console.log)");
// 		fs.appendFileSync("c:\\temp\\vscode.log", "g_connection.onInitialize");
// 		console.log("g_connection.onInitialize (console.log)");

// 		let caps = params.capabilities;

// 		//g_hasConfigCap = !!caps.workspace?.configuration;
// 		g_hasWorkspaceFolderCap = !!caps.workspace?.workspaceFolders;
// 		g_hasDiagnosticRelationInfoCap = !!caps.textDocument?.publishDiagnostics?.relatedInformation;

// 		return {
// 			capabilities: {
// 				textDocumentSync: TextDocumentSyncKind.Full/*,
// 				completionProvider: {
// 					resolveProvider: true
// 				}*/
// 			}
// 		};
// 	});

// 	g_connection.onInitialized(() =>
// 	{
// 		// if (g_hasConfigCap)
// 		// {
// 		// 	g_connection.client.register(DidChangeConfigurationNotification.type, undefined);
// 		// }
// 		if (g_hasWorkspaceFolderCap)
// 		{
// 			g_connection.workspace.onDidChangeWorkspaceFolders(_event =>
// 			{
// 				g_connection.console.log("Workspace folder changed.");	// TODO
// 			});
// 		}
// 	});
// }

// g_documents.onDidClose(e =>
// {
// });

// g_documents.onDidChangeContent(e =>
// {
// 	g_connection.console.log("g_documents.onDidChangeContent");

// 	validateDocument(e.document);
// });

// async function validateDocument(doc: TextDocument): Promise<void>
// {
// 	g_connection.console.log(`"Validating document: ${doc.uri}`);

// 	let code = doc.getText();
// 	let pattern = /\b[A-Z_]{2,}\b/g;
// 	let diags: Diagnostic[] = [];
// 	let m: RegExpExecArray | null;

// 	while ((m = pattern.exec(code)))
// 	{
// 		let diag: Diagnostic = {
// 			severity: DiagnosticSeverity.Warning,
// 			range: {
// 				start: doc.positionAt(m.index),
// 				end: doc.positionAt(m.index + m[0].length)
// 			},
// 			message: `${m[0]} is all uppercase.`,
// 			source: 'dkcode'
// 		};

// 		if (g_hasDiagnosticRelationInfoCap)
// 		{
// 			diag.relatedInformation = [
// 				{
// 					location: {
// 						uri: doc.uri,
// 						range: Object.assign({}, diag.range)
// 					},
// 					message: "Spelling matters"
// 				},
// 				{
// 					location: {
// 						uri: doc.uri,
// 						range: Object.assign({}, diag.range)
// 					},
// 					message: "Especially for names"
// 				}
// 			];
// 		}
// 		diags.push(diag);
// 	}

// 	g_connection.sendDiagnostics({ uri: doc.uri, diagnostics: diags });
// }

