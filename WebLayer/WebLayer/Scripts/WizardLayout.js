//globals
var rightRail, header, workarea;
var $RCUI = rcBaseWindow.$RCUI;
var helpTopic;

function pageBlur() {
    if ($RCUI.isDirty) {
        pageExit();
        $RCUI.isDirty = false;
    }
}

window.onunload = pageBlur;

function insertScriptTag(url) 
{
    document.write('<scri' + 'pt src="' + url + '" type="text\/javascript"><\/script>');
}
insertScriptTag('.\/dhtml\/codebase\/dhtmlx.js');
insertScriptTag('.\/scripts\/json\/json2.js');
insertScriptTag('.\/scripts\/jsonpath-0.8.0.js');
insertScriptTag('.\/jquery\/jquery-1.7.1.min.js');

function pageRender(localPageRenderer) {
    initializeCampaignClient();
    var helpContent = ''
    if (helpTopic != undefined) {
        if (window.ApnxApp) {
            var appHelpTopic = helpTopic + 'APNX'
            var appHelpContent = $RCUI.resources('Campaign', 'Help' + appHelpTopic);
            if (appHelpContent != undefined) {
                helpTopic = appHelpTopic;
            }
        }
    }
    else {
        helpTopic = 'Default';
    }
    $RCUI.contextWindow().innerHTML = $RCUI.resources('Campaign', 'Help' + helpTopic);

    dhtmlx.image_path = './dhtml/codebase/imgs/';
    var main_layout = new dhtmlXLayoutObject(document.body, '1C');
    main_layout.setSkin('dhx_web');

    workarea = main_layout.cells('a');
    workarea.hideHeader();
    var counter2 = new Date();
    localPageRenderer();
}