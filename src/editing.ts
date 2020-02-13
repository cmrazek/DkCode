import * as vscode from 'vscode';
import * as path from 'path';

function getProbeMonthName(month: number) : string
{
    switch (month)
    {
        case 0: return "Jan";
        case 1: return "Feb";
        case 2: return "Mar";
        case 3: return "Apr";
        case 4: return "May";
        case 5: return "Jun";
        case 6: return "Jul";
        case 7: return "Aug";
        case 8: return "Sep";
        case 9: return "Oct";
        case 10: return "Nov";
        case 11: return "Dec";
        default: return "";
    }
}

function formatProbeDate(dt: Date) : string
{
    if (dt == null) return "";
    return dt.getDate().toString().padStart(2, '0') + getProbeMonthName(dt.getMonth()) + dt.getFullYear().toString().padStart(4, '0');
}

export function addFileHeader(textEditor: vscode.TextEditor, edit: vscode.TextEditorEdit)
{
    console.log("Adding file header");

    let initials = <string>vscode.workspace.getConfiguration().get("dkcode.userInitials");
    let workItemId = <string>vscode.workspace.getConfiguration().get("dkcode.workItemId");
    let eol = textEditor.document.eol == vscode.EndOfLine.LF ? "\n" : "\r\n";

    let hdr = "/***************************************************************************************************" + eol;
    hdr += " * File Name: " + path.basename(textEditor.document.fileName) + eol;
    hdr += " *  " + eol;
    hdr += " * " + eol;
    hdr += " * Modification History:" + eol;
    hdr += " *  Date      Who Work Item #  Description of Changes" + eol;
    hdr += " *  --------- --- ------------ ---------------------------------------------------------------------" + eol;
    let commentLine = " *  " + formatProbeDate(new Date()) + " " + initials.padEnd(3, ' ') + " " + workItemId.padEnd(12, ' ') + " ";
    hdr += commentLine + eol;
    hdr += " **************************************************************************************************/" + eol;
    hdr += eol;

    textEditor.edit(e =>
    {
        e.insert(new vscode.Position(0, 0), hdr);
    }).then(() =>
    {
        let cursorPos = new vscode.Position(7, commentLine.length);
        textEditor.selection = new vscode.Selection(cursorPos, cursorPos);
        textEditor.revealRange(new vscode.Range(new vscode.Position(0,0), new vscode.Position(0,0)), vscode.TextEditorRevealType.AtTop);
    })
}
