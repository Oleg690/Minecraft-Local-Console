let theme2 = "ace/theme/tomorrow_night_eighties"
let theme3 = "ace/theme/idle_fingers"

let cssEditor = ace.edit(css)

cssEditor.setTheme(theme2)
cssEditor.session.setMode('ace/mode/properties')
cssEditor.setFontSize(18)

cssEditor.setValue(`eula=true`, 1)

let jsEditor = ace.edit(js)

jsEditor.setTheme(theme3)
jsEditor.session.setMode('ace/mode/json')

jsEditor.setOptions({
    fontSize: "18pt"
});

jsEditor.setValue(`[]`, 1)

function updateResult() {
    let cssCode = "<style>" + cssEditor.getValue() + "</style>"
    let jsCode = "<script>" + jsEditor.getValue() + "</script>"

    let previewWindow = document.getElementById("preview-window").contentWindow.document;

    previewWindow.open()
    previewWindow.write(cssCode + jsCode)
    previewWindow.close()
}