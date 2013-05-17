insertScriptTag('.\/dhtml\/codebase\/dhtmlx.js');
insertScriptTag('.\/scripts\/json\/json2.js');
insertScriptTag('.\/scripts\/jsonpath-0.8.0.js');
insertScriptTag('.\/jquery\/jquery-1.7.1.min.js');
var chromeOn = !window.ApnxApp;
window.name = 'rcBaseWindow';

function preNavigationCheck(){
//TODO: Implement dirty page check
    return true;
}

function toolBarClickHandler(id) {
    if (!preNavigationCheck()) {
        return; //TODO handle dirty page better
    }
    switch (id)
    {
        case 'homeButton':
            $RCUI.breadCrumbs.clear();
            window.location = '\/company.html';
            break;
        default:
            var targetObjId = $RCUI.breadCrumbs.crumbs[id.split(":")[1]].id;
            var targetObjType = $RCUI.breadCrumbs.crumbs[id.split(":")[1]].type;
            switch (targetObjType.toLowerCase()) {
                case 'agency':
                    window.location = '\/company.html?agency=' + targetObjId;
                    break;
                case 'advertiser':
                    window.location = '\/campaign.html?agency=' + $RCUI.agency + '&advertiser=' + targetObjId;
                    break;
                case 'campaign':
                    window.location = '\/campaign.html?agency=' + $RCUI.agency + '&advertiser=' + $RCUI.advertiser + '&campaign=' + targetObjId;
                    break;
                default:
                    window.location = '\/home.html';
                    break;
            };
            break;
    };
}

function pageRender(localPageRenderer) {

    globalInitialize();
    dhtmlx.image_path = './dhtml/codebase/imgs/';

    var main_layout = new dhtmlXLayoutObject(document.body, '4I');
    main_layout.setSkin('dhx_web');
    main_layout.setAutoSize('a;b;d', 'b;c');

    Nav = main_layout.cells('a');
    Nav.setHeight('40');
    Nav.fixSize(1, 1);
    Nav.hideHeader();

    var navLayout = Nav.attachLayout('3W');
    var header = navLayout.cells('a');
    header.fixSize(1, 1);
    header.setWidth(250);
    header.hideHeader();
    header.attachHTMLString('<img src=".\/images\/rclogo.png" height="30" width="235" \/>')

    var navBar = navLayout.cells('b');
    navBar.fixSize(1, 1);
    navBar.hideHeader();

    var logout = navLayout.cells('c');
    logout.fixSize(1, 1);
    logout.setWidth(75);
    logout.hideHeader();

    if (chromeOn) {
        logout.attachHTMLString('<div style="float:right;padding:8;" onclick="location.href=\'\/LogOff.aspx\'">Logout</div>')
    }

    Toolbar = navBar.attachToolbar();
    Toolbar.setSkin('dhx_web');
    Toolbar.addButton('homeButton', 3, 'Home');
    Toolbar.attachEvent('onClick', toolBarClickHandler);
    $RCUI.breadCrumbs.showBreadCrumbs();

    workarea = main_layout.cells('b');
    workarea.hideHeader();

    content = main_layout.cells('c');
    content.setWidth('210');
    content.hideHeader();

    var foo1 = document.createElement('div');
    foo1.setAttribute('style', 'padding:8px;background:#c0c0c0;-moz-border-radius: 22px;border-radius: 22px;');
    foo1.innerHTML = '<div style="font:12pt Tahoma;font-weight: bold;color:#880000;padding-bottom:12px;">' + $RCUI.resources('Campaign', 'HelpDefaultTitle') + '<\/div>';
    foo1.innerHTML += '<div id="rrContent" style="font:13px Tahoma;padding:2px;">' + $RCUI.resources('Campaign', 'HelpDefault') + '<\/div>';
    content.attachObject(foo1);

    footer = main_layout.cells('d');
    footer.setHeight('10');
    footer.hideHeader();
    footer.fixSize(1, 1);

    var footerHTML = document.createElement('div');
    footerHTML.setAttribute('style', 'font:Tahoma;font-size:8pt;padding:3px;background:#c0c0c0;color:#880000;');
    if (chromeOn) {
        footerHTML.setAttribute('onclick', 'displayJSONPage();');
    }
    footerHTML.innterHTML = '';
    var preCache = ''; //used to precache the measures.js file
    if (getQueryString()['advertiser'] != undefined && getQueryString()['campaign'] != undefined) {
        preCache += '<img style="display:none" height="1" width="1" src="\/api\/data\/measures.xml?mode=all&company={0}&campaign={1}" \/>'.format(getQueryString()['advertiser'], getQueryString()['campaign']);
    }
    footerHTML.innerHTML += '<div id="rcFooter" width="20%" style="float:left;">EULA | Privacy | Contact Us<\/div>';
    footerHTML.innerHTML += '<div id="rcLights" width="20%" style="float:right;">' +  preCache + '<iframe src="ping.html" frameborder="0" marginheight="0" marginwidth="0" height="5" width="5" \/><\/div>';
    footerHTML.innerHTML += '<div id="rcDebug" width="60%" style="text-align:center;font-size:7pt;overflow:hidden;height:10px;">...<\/div>';
    footer.attachObject(footerHTML);

    localPageRenderer();
}

function displayJSONPage() {
    if ($RCUI.campaign != undefined) {
        location.href = 'JSON.html?agency=' + $RCUI.agency + '&advertiser=' + $RCUI.advertiser + '&campaign=' + $RCUI.campaign;
        return;
    }
    if ($RCUI.advertiser != undefined) {
        location.href = 'JSON.html?agency=' + $RCUI.agency + '&advertiser=' + $RCUI.advertiser;
        return;
    }
    if ($RCUI.agency != undefined) {
        location.href = 'JSON.html?agency=' + $RCUI.agency;
        return;
    }
}