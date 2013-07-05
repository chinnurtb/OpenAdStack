//globals
var header, Nav, workarea, navTree, content, footer, debugArea;
var Toolbar;
var $RCAPI = {
    URI: {
        UserCreate: '\/api\/entity\/user',
        UserGet: '\/api\/entity\/user\/{0}',
        UsersGet: '\/api\/entity\/user',
        UserUpdate: '\/api\/entity\/user\/{0}',
        UserInvite: '\/api\/entity\/user\/{0}?Message=Invite',
        UserConfirm: '\/api\/entity\/user\/{0}?Message=verify',

        AgencyCreate: '\/api\/entity\/company',
        AdvertiserCreate: '\/api\/entity\/company\/{0}\/company',
        CompanyGetByUser: '\/api\/entity\/company\/',
        CompanyGet: '\/api\/entity\/company\/{0}',
        CompanyUpdate: '\/api\/entity\/company\/{0}',
        
        BillingInfoUpdate: '\/api\/entity\/company\/{0}?Message=UpdateBillingInfo',

        CompanyAssociate: '\/api\/entity\/company\/{0}?Message=addadvertiser',
        CompanyGetAssociations: '\/api\/company\/associations\/{0}',

        CampaignCreate: '\/api\/entity\/company\/{0}\/campaign',
        CampaignGet: '\/api\/entity\/company\/{0}\/campaign\/{1}?Flags=WithAssociations',
        CampaignCampaignForCompany: '\/api\/entity\/company\/{0}\/campaign',
        CampaignUpdate: '\/api\/entity\/company\/{0}\/campaign\/{1}',

        CampaignGetCreatives: '\/api\/entity\/company\/{0}\/campaign\/{1}?Flags=Creatives',
        CampaignGetValuationPending: '\/api\/entity\/company\/{0}\/campaign\/{1}?Valuations=pending',
        CampaignGetBlob: '\/api\/entity\/company\/{0}\/campaign\/{1}?BLOB={2}',
        
        CreativeCreate: '\/api\/entity\/company\/{0}\/creative',
        CreativeGet: '\/api\/entity\/company\/{0}\/creative\/{1}',
        CreativeUpdate: '\/api\/entity\/company\/{0}\/creative\/{1}',

        CreativeGetCampaign: '\/api\/{0}\/creative/{1}',        
        CreativeCampaignAssociate: '\/api\/entity\/company\/{0}\/campaign\/{1}?Message=AddAssociation',
        CreativeCampaignRemove: '\/api\/entity\/company\/{0}\/campaign\/{1}?Message=RemoveAssociation',

        ApnxUserRegister: '\/api\/apnx\/register',
        ApnxGetAdvertisers: '\/api\/apnx\/advertisers',
        ApnxGetAdvertiserCreatives: '\/api\/apnx\/creatives?Company={0}&Campaign={1}',

        DataMeasuresDictionary: '\/api\/data\/measures.js?mode=all&company={0}&campaign={1}',
        DataMeasuresDictionaryWithPaging: '\/api\/data\/measures.js?mode=paged&company={0}&campaign={1}&offset={2}&count={3}',
        DataMeasuresDictionaryWithIds: '\/api\/data\/measures.js?mode=all&company={0}&campaign={1}&ids={2}',
        DataMeasuresDictionaryWithInclude: '\/api\/data\/measures.js?mode=all&company={0}&campaign={1}&include={2}'
    }
};

function insertScriptTag(url) {
    document.write('<scri' + 'pt src="' + url + '" type="text\/javascript"><\/script>');
}

//extend string to have format method
String.prototype.format = function () {
    var args = arguments;
    return this.replace(/\{\{|\}\}|\{(\d+)\}/g, function (m, n) {
        if (m === "{{") { return "{"; }
        if (m === "}}") { return "}"; }
        return args[n];
    });
};

// This function removes non-numeric characters
String.prototype.stripNonNumeric = function (str) {
    str += '';
    var rgx = /^\d|\.|-$/;
    var out = '';
    for (var i = 0; i < str.length; i++) {
        if (rgx.test(str.charAt(i))) {
            if (!((str.charAt(i) === '.' && out.indexOf('.') !== -1) ||
             (str.charAt(i) === '-' && out.length !== 0))) {
                out += str.charAt(i);
            }
        }
    }
    return out;
};

// extend date with ISO converter
// ISO-8601 Date Matching
var reIsoDate = /^(\d{4})-(\d{2})-(\d{2})((T)(\d{2}):(\d{2})(:(\d{2})(\.\d*)?)?)?(Z)?$/;
Date.parseISO = function (val) {
    var m;
    m = typeof val === 'string' && val.match(reIsoDate);
    if (m) return new Date(Date.UTC(+m[1], +m[2] - 1, +m[3], +m[6] || 0, +m[7] || 0, +m[9] || 0, parseInt((+m[10]) * 1000) || 0));
    return null;
};

// override the toISOString so it works in IE(older than IE9 standards)
Date.prototype.toISOString = Date.prototype.toISOString || function () {
    return this.getUTCFullYear() + "-"
    + ("0" + this.getUTCMonth() + 1 + "-").slice(-3)
    + ("0" + this.getUTCDate() + "T").slice(-3)
    + ("0" + this.getUTCHours() + ":").slice(-3)
    + ("0" + this.getUTCMinutes() + ":").slice(-3)
    + ("0" + this.getUTCSeconds() + ".").slice(-3)
    + ("00" + this.getUTCMilliseconds() + "Z").slice(-4);
};

// extend date with to/from UTC methods
Date.prototype.shiftToUTC = function () {
    // Add the timezone offset to the local date to create a UTC date
    var offset = this.getTimezoneOffset() * 60000;
    return new Date(this.getTime() + offset);
};

Date.prototype.shiftFromUTC = function () {
    // Subtract the timezone offset from a local date (representing a UTC date)
    var offset = this.getTimezoneOffset() * 60000;
    return new Date(this.getTime() - offset);
};

//extend number with format
/**
* Formats the number according to the ‘format’ string;
* comma is inserted after every 3 digits.
*  note: there should be only 1 contiguous number in the format,
* where a number consists of digits, period, and commas
*        any other characters can be wrapped around this number, including ‘$’, ‘%’, or text
*        examples (123456.789):
*          ‘0′ - (123456) show only digits, no precision
*          ‘0.00′ - (123456.78) show only digits, 2 precision
*          ‘0.0000′ - (123456.7890) show only digits, 4 precision
*          ‘0,000′ - (123,456) show comma and digits, no precision
*          ‘0,000.00′ - (123,456.78) show comma and digits, 2 precision
*          ‘0,0.00′ - (123,456.78) shortcut method, show comma and digits, 2 precision
*/
Number.prototype.format = function (format) {
    var hasComma = -1 < format.indexOf(','),
    psplit = format.stripNonNumeric().split('.'),
    that = this;
    // compute precision
    if (1 < psplit.length) {
        // fix number precision
        that = that.toFixed(psplit[1].length);
    }
    // error: too many periods
    else if (2 < psplit.length) {
        throw ('NumberFormatException: invalid format, formats should have no more than 1 period: ' + format);
    }
    // remove precision
    else {
        that = that.toFixed(0);
    }

    // get the string now that precision is correct
    var fnum = that.toString();
    // format has comma, then compute commas
    if (hasComma) {
        // remove precision for computation
        psplit = fnum.split('.');
        var cnum = psplit[0],
        parr = [],
        j = cnum.length,
        m = Math.floor(j / 3),
        n = cnum.length % 3 || 3; // n cannot be ZERO or causes infinite loop

        // break the number into chunks of 3 digits; first chunk may be less than 3
        for (var i = 0; i < j; i += n) {
            if (i !== 0) { n = 3; }
            parr[parr.length] = cnum.substr(i, n);
            m -= 1;
        }
        // put chunks back together, separated by comma
        fnum = parr.join(',');

        // add the precision back in
        if (psplit[1]) { fnum += '.' + psplit[1]; }
    }
    // replace the number portion of the format with fnum
    return format.replace(/[\d,?\.?]+/, fnum);
};

//utility function copied from w3cschools
function getQueryString() {
    var result = {}, queryString = location.search.substring(1), re = /([^&=]+)=([^&]*)/g, m;
    while (m = re.exec(queryString)) {
        result[decodeURIComponent(m[1])] = decodeURIComponent(m[2]);
    }
    return result;
}

//Load Resource Lookup
function LookUpResource(object, resource) {
    var resources = {
        Campaign: {
            EnabledWizardMenu: [{ DisplayText: 'Setup', Href: 'campaignCreate.html' },
            { DisplayText: 'Measures', Href: 'personaCreate.html' },
            { DisplayText: 'Measure Weights', Href: 'BaseValuations.html' },
            { DisplayText: 'Valuations', Href: 'Overrides.html' },
            { DisplayText: 'Assign Creatives', Href: 'Creatives.html' },
            { DisplayText: 'Summary', Href: 'Report.html' },
            { DisplayText: 'Delivery Review', Href: 'ReportDelivery.html' }
            ],
            DefaultWizardMenu: [{ DisplayText: 'New Campaign', Href: 'campaignCreate.html'}],
            HelpDefaultTitle: 'Help',
            HelpCampaignCreate: '<b>Input a name for your campaign.<br><br>Input your Total Budget.<br><br>Input your Max Average CPM. </b>This represents the maximum average CPM you want this campaign to stay under. This also gives a sense of the maximum volume you will ever run on this campaign, since budget / CPM * 1000 gives you the number of maximum impressions you\'re likely to see in this campaign. Note that this is Maximum average, not a target average.  We strongly recommend that this maximum be realistic - if you set this to a very low number, you will make it very hard for the campaign to spend budget on anything but very low cost inventory. <b>Note</b> that data costs are included in this CPM average, so the average price is media cost + data cost.<br><br><b>Input your campaign flight dates</b> (start and end dates).',
            HelpCampaignCreateAPNX: '<b>Input a name for your campaign.<br><br>Input your Total Budget.<br><br>Input your Max Average CPM. </b>This represents the maximum average CPM you want this campaign to stay under. This also gives a sense of the maximum volume you will ever run on this campaign, since budget / CPM * 1000 gives you the number of maximum impressions you\'re likely to see in this campaign. Note that this is Maximum average, not a target average.  We strongly recommend that this maximum be realistic - if you set this to a very low number, you will make it very hard for the campaign to spend budget on anything but very low cost inventory. <b>Note</b> that data costs are included in this CPM average, so the average price is media cost + data cost.<br><br><b>Input your campaign flight dates</b> (start and end dates).',
            HelpDefault: 'From this screen you can access existing campaigns, or access the New Campaign screen. Click on the campaign you\'d like to enter, or on New Campaign.',
            HelpMeasureValuation: 'On this screen, you will set a CPM value of each measure that you\'ve added to the system. Keep in mind that the values you assign here will lead to specific price changes of each piece of inventory - so think of this as the first step in setting prices or valuation of inventory. It\'s fine to set multiple measures to the same CPM value, but ideally each measure should be valued uniquely.<br><br>Use the sliders for broad changes, or type in an explicit CPM price and hit enter for direct control. There are no limits on the price, and some measures may well be valuable and set to a significantly high CPM depending on your campaign goals.',
            HelpReview: 'This page describes the rules of how we will deliver your campaign.<br><br>The right side of the page shows the segments you selected for creating your campaign, and the rules for how we can combine segments to create inventory. You\'ll see phrases like "always one of these", "Optionally one of these", and "Optionally one or more of these".<br><br>On the bottom left of the page are the overall campaign "business terms".  This includes the budget, the combinations (the number of potential line items that might exist), the maximum average CPM you input, and the flight dates you signed up for.  These can all be adjusted on the setup screen.<br><br>On the top left side of the page is the layer model showing how we\'ll attempt to distribute your budget. At the bottom is standard inventory, which is inventory you can buy today that has between 1-3 targeting segments applied. Each layer becomes progressively more targeted as you move upward. Depending on how you set up your campaign, the system will optimize how it allocates budget in different ways, affecting the budget allocation on this page. We always try to deliver budget inventory we can find during the life of the campaign.',
            HelpMeasureCreate: 'From this page, select the measures you want to add to your campaign. You can also group measures together into "or groups" and set measures or groups as "required" or Locked.<br><br>On the left side of the screen, find measures you\'d like to add to your campaign, to add them to your campaign, either double-click them, or select and click the ">>" button to move them to the right. You can search for measures using the Search Filter box at the top of that screen. You must add at least 8 measures to your campaign to get the real value of Rare Crowds. Please limit your campaign to no more than 20 measures (you can go over this limit, but rendering your valuations screen may take a long time). You can always set up multiple campaigns if you want to try different approaches.<br><br>On the right side of the screen, you can group measures together by clicking the "select" box, then clicking the "group" button groups can also be given custom names. This is useful for measures that are mutually exclusive - e.g. Age Ranges, Gender, Geography, etc...<br><br>You can also lock measures or groups that are required to ensure all impressions contain them. <b>NOTE:</b> By locking (requiring) specific measures or groups of measures you may significantly reduce the amount of available inventory - in some cases making it impossible to find. If any individual measure is by nature rare, take into account that you may want to avoid locking the measure. Please contact Rare Crowds support for assistance here.',
            HelpInventoryDefinition: '',
            HelpCreative: 'From this screen you can upload new creatives (3rd Party Ad Tags, Images or Flash) or you can select existing creatives from other campaigns and assign them to this campaign.',
            HelpNodeOverride: '<table width="100%" style="font:13px Tahoma">' +
                '<tr><td style="background:#ffff00;height:20px;width:20px" /></tr><tr><td>Explicit Valuation</td></tr>' +
                '<tr><td style="background:#000000;height:20px;width:20px" /></tr><tr><td>Default Valuation</td></tr>' +
                '<tr><td style="background:#0080ff;height:20px;width:20px" /></tr><tr><td>Informed Valuation</td></tr></table>' +
                '<br><b>Note:</b> This screen may take some time to load if you\'ve chosen more than 20 measures, please be patient.<br><br>When you visit this screen for the first time during a campaign setup, you will be presented with a wizard that requests specific valuations (ideal and maximum) for 9-12 inventory definitions.  Giving pricing inputs for the wizard is required, this should only take a few minutes.<br><br>Once you\'ve completed this wizard, you can leave this page and be done with your campaign configuration.  However, we do give you additional valuation control for advanced user scenarios as well, should you choose to use them. THIS IS NOT REQUIRED.<br><br>This screen shows the full graph of possible combinations of measures that could be found in an inventory source. Each square is an inventory definition (line item) that will have a price assigned. You can select any node, and input your ideal and maximum valuation on any node.  Note that all the parents and children of that node are also highlighted any time you select a node.',
            
            HelpReportDelivery: '<table width="100%" style="font:13px Tahoma">' +
                '<tr><td style="background:#804020;height:20px;width:20px" /></tr><tr><td>Never Exported</td></tr>' +
                '<tr><td style="background:#FF0000;height:20px;width:20px" /></tr><tr><td>Known Non-Delivery</td></tr>' +
                '<tr><td style="background:#300000;height:20px;width:20px" /></tr><tr><td>Reduced Priority</td></tr>' +
                '<tr><td class="deliveredGradient" style="height:20px;width:20px" /></tr><tr><td>Previously Delivered</td></tr>' +
                '<tr><td class="deliveryGradient" style="height:20px;width:20px" /></tr><tr><td>Currently Delivering</td></tr>' +
                '<tr><td style="background:#880088;height:20px;width:20px" /></tr><tr><td>Excluded from Delivery</td></tr>' +
                '<tr><td style="background:#0000ff;height:20px;width:20px" /></tr><tr><td>First Time Delivering</td></tr></table>'
        },
        Company: {
            DefaultMenu: [{ DisplayText: 'New Account', Href: '', Action: 'Agency' },
                { DisplayText: 'Manage Users', Href: 'user.html', Action: 'User' },
                { DisplayText: 'Update Account', Href: '', Action: 'UpdateAgency'}],
            AgencyMenu: [{ DisplayText: 'New Customer', Href: '', Action: 'Advertiser' },
                { DisplayText: 'Manage Users', Href: 'user.html', Action: 'User' },
                { DisplayText: 'Update Customer', Href: '', Action: 'UpdateAdvertiser'}],
            ApnxMenu: [{ DisplayText: 'Add Advertiser', Href: '', Action: 'Advertiser' }],
            IntroText: '',
            HelpDefaultTitle: 'Help',
            HelpDefault: 'On this page you can set up and access top-level accounts. These top level accounts can create and manage sub-accounts for their customers. This page is for internal Rare Crowds users - employees of the company.'
        },
        User: {
            DefaultMenu: [{ DisplayText: 'New User', Href: '', Action: 'NewUser' },
                { DisplayText: 'Update User', Href: '', Action: 'UpdateUser'}],
            IntroText: '',
            HelpDefaultTitle: 'Help',
            HelpDefault: ''
        }
    };
    return resources[object][resource];
}

function getBaseWindowRef() {
    var baseWindowCandidate = window;
    var keepGoing = true;
    while (keepGoing){
        try {
            var parentLocation = '' + baseWindowCandidate.parent.location;
            baseWindowCandidate = baseWindowCandidate.parent;
            if (baseWindowCandidate == window.top) {
                keepGoing = false;
            }
        }
        catch (e) {
            keepGoing = false;
        }
    }
    return baseWindowCandidate;
}

var rcBaseWindow = getBaseWindowRef();

//RC UI object
function breadCrumbs() {
    if (sessionStorage.getItem("crumbs")) {
        this.crumbs = JSON.parse(sessionStorage.getItem("crumbs"));
    }
    else {
        this.crumbs = {};
        return;
    }
}

breadCrumbs.prototype.showBreadCrumbs = function () {
    if (window.self == rcBaseWindow) {//only if outer chrome
        if (!window.ApnxApp && ($RCUI.querystring["agency"]) && ($RCUI.querystring["agency"] != "undefined")) {
            Toolbar.addButton("crumb:" + $RCUI.querystring["agency"], 10, this.crumbs[$RCUI.querystring["agency"]]["name"]);
        }
        if (($RCUI.querystring["advertiser"]) && ($RCUI.querystring["advertiser"] != "undefined")) {
            Toolbar.addButton("crumb:" + $RCUI.querystring["advertiser"], 50, this.crumbs[$RCUI.querystring["advertiser"]]["name"]);
        }
        if (($RCUI.querystring["campaign"]) && ($RCUI.querystring["campaign"] != "undefined")) {
            Toolbar.addButton("crumb:" + $RCUI.querystring["campaign"], 100, this.crumbs[$RCUI.querystring["campaign"]]["name"]);
        }
    }
};
breadCrumbs.prototype.add = function (name, type, id) {
    this.crumbs[id] = { "type": type, "id": id, "name": name };
    sessionStorage.setItem("crumbs", JSON.stringify(this.crumbs));
};
breadCrumbs.prototype.remove = function (id) {
    //placeholder, later use
    throw "NoIMPL";
};
breadCrumbs.prototype.clear = function () {
    Toolbar.clearAll();
    sessionStorage.setItem("crumbs", []);
    this.crumbs = {};
};
breadCrumbs.prototype.crumbs = {};


function $RCAjax(resource, messageBody, verb, query, filter, isAsync, successHandler, errorHandler) {
    function onSuccess(serverData, statusText, responseJson) {
        endTime = new Date();
        that.responseData = serverData;
        $RCUI.debugReport.record(getDebugJsonDiv(responseJson));
        $RCUI.debugReport.record(endTime - startTime);
        if (responseJson.status == 202 && that.retryCount < 3 && that.verb == 'GET') { //do a retry on "accepted"GETs
            that.retryCount++;
            executeCall();
        }
        else { //let outer handler take it from here
            if (successHandler != null) {
                successHandler();
            }
        }
    }

    function onError(responseJson, errorType, errorText) {
        endTime = new Date();
        that.responseData = responseJson;
        $RCUI.debugReport().innerHTML = getDebugJsonDiv(responseJson);
        $RCUI.debugReport().innerHTML += (endTime - startTime);
        if (responseJson.status == 400 && that.retryCount < 3 && that.verb == 'GET') { //do a retry on "Bad Requests" GETs and auth error
            that.retryCount++;
            executeCall();
        }
        else if (window.ApnxApp && responseJson.status == 401) {
            var authError = responseJson.getResponseHeader("WWW-Authenticate");
            if (window.ApnxApp && authError == "INVALID USER") {
                setTimeout(function () { rcBaseWindow.location = '\/registration.html'; }, 1500);
            }
            else {
                alert("ACCESS DENIED");
            }
        }
        else { //let outer handler take it from here
            if (errorHandler != null) {
                errorHandler(responseJson, errorType, errorText, that.resource);
            }
        }
    }

    function getDebugJsonDiv(debugJson) {
        return '<div id="hiddenRCdebug" style="width:400px;height:200px;position:absolute;top:100px;overflow:scroll;display:none">' + JSON.stringify(debugJson) + '</div> ';
    }

    function executeCall() {
        $.ajax({
            type: verb,
            async: isAsync,
            beforeSend: function (xhr) { if (xhr.overrideMimeType) { xhr.overrideMimeType("application/json"); } },
            url: resource,
            contentType: 'application/json',
            data: messageBody,
            dataType: 'json',
            success: onSuccess,
            error: onError,
            timeout: 35000
        });
    }

    //Body of constructor
    var endtime = 0;
    var startTime = new Date();
    //add query string and filter string to the resource before passing to Jquery
    if (query || filter) { resource += '?'; }
    if (query) { resource += query; }
    if (filter) { resource += '&' + filter; }

    this.resource = resource;
    this.messageBody = messageBody;
    this.verb = verb;
    this.retryCount = 0;
    var that = this;
    executeCall();
}

function debugContainer() {
    return rcBaseWindow.document.getElementById('rcDebug');
}

function debugRecordReport(str) {
    rcBaseWindow.document.getElementById('rcDebug').innerHTML = str;
}

function contextContainer() {
    return rcBaseWindow.document.getElementById('rrContent');
}

var $RCUI;
function globalInitialize() {
    $RCUI = {
        "breadCrumbs": new breadCrumbs(),
        "isDirty": false,
        "querystring": getQueryString(),
        "contextWindow": contextContainer,
        "debugReport": debugContainer,
        "resources": LookUpResource,
        "agency": getQueryString()["agency"],
        "advertiser": getQueryString()["advertiser"],
        "campaign": getQueryString()["campaign"]
    };
    $RCUI.debugReport.record = debugRecordReport;
}

function setCookie(name, value) {
    document.cookie = name + '=' + value + '; domain=' + document.domain + '; path=/';
}

function getCookie(name) {
    var search = name + "=";
    if (document.cookie.length > 0) {
        var offset = document.cookie.indexOf(search);
        if (offset != -1) {
            offset += search.length;
            var end = document.cookie.indexOf(";", offset);
            if (end == -1) end = document.cookie.length;
            return (document.cookie.substring(offset, end));
        }
    }
};


function lazyDictionary(resource, pageSize, initalFilter) {
    function get(key) {
        if (internalDictionary[key] == undefined) {
            executeCall($RCAPI.URI.DataMeasuresDictionaryWithIds.format($RCUI.advertiser, $RCUI.campaign, key), false);
        }
        return internalDictionary[key];
    }

    function getDictionary() {
        return internalDictionary;
    }

    function onSuccess(serverData, statusText, responseJson) {
        $.extend(internalDictionary, serverData);
        responseCount++;
        $RCUI.debugReport.record("Measures Page Loaded " + responseCount);
        var count = 0;
        for (var member in serverData) {
            count++;
            break;
        }
        if (attemptCount <= maxPages && count > 0 && fullLoad) {
            setTimeout(function () { timerCall.call(that); }, 10);
        }
    }

    function onError(responseJson, errorType, errorText) {
        $RCUI.debugReport.record("Measures: " + errorText + " Retrying");
        if (attemptCount <= maxPages && fullLoad) {
            setTimeout(function () { timerCall.call(that); }, 10);
        }
    }

    function executeCall(resource, isAsync) {
    //TODO: needs to call different onSuccess if only getting single measure
        $.ajax({
            type: 'GET',
            async: isAsync,
            beforeSend: function (xhr) { if (xhr.overrideMimeType) { xhr.overrideMimeType("application/json"); } },
            url: resource,
            contentType: 'application/json',
            dataType: 'json',
            success: onSuccess,
            error: onError,
            timeout: 100000
        });
    }

    function timerCall() {
        attemptCount++;
        executeCall($RCAPI.URI.DataMeasuresDictionaryWithPaging.format($RCUI.advertiser, $RCUI.campaign, (attemptCount -1) * pageSize, pageSize), true)
    }

    function load() {
        fullLoad = true;
        setTimeout(function () { timerCall.call(that); }, 10);
    }

    function loadByFilterSync(filter) {
        executeCall($RCAPI.URI.DataMeasuresDictionaryWithInclude.format($RCUI.advertiser, $RCUI.campaign, filter), false);
    }

    var maxDictionarySize = 50000;
    var internalDictionary = {};
    var that = this;
    var maxPages = Math.round(maxDictionarySize / pageSize);
    var responseCount = 0;
    var attemptCount = 0;
    var fullLoad = false; // determined if we will load the entire dictionary or just the initial filter
    this.Get = get;
    this.toDictionary = getDictionary;
    this.initialize = load;
    this.loadByFilter = loadByFilterSync;

    if (initalFilter != '') {
        executeCall($RCAPI.URI.DataMeasuresDictionaryWithIds.format($RCUI.advertiser, $RCUI.campaign, initalFilter), false);
    }
    else {
        timerCall();
    }
}

// Reads a file and calls the handler when finished
function readFileInput(inputElement, maxFileSize, onLoadEnd) {
    if (inputElement.files.length == 0) {
        alert("Please select a file.");
        return;
    }

    var file = inputElement.files[0];
    if (maxFileSize > 0 && file.size > maxFileSize) {
        alert("The file " + file.name + "is too large.\nPlease select a smaller file.\nMaximum: " + maxFileSize + " bytes");
        return;
    }

    var reader = new FileReader();
    reader.fileName = file.name;
    reader.onloadend = onLoadEnd;
    reader.readAsBinaryString(file);
}

var keyStr = "ABCDEFGHIJKLMNOP" +
        "QRSTUVWXYZabcdef" +
        "ghijklmnopqrstuv" +
        "wxyz0123456789+/" +
        "=";

function encode64(input) {
    var output = "";
    var chr1, chr2, chr3 = "";
    var enc1, enc2, enc3, enc4 = "";
    var i = 0;

    do {
        chr1 = input.charCodeAt(i++);
        chr2 = input.charCodeAt(i++);
        chr3 = input.charCodeAt(i++);

        enc1 = chr1 >> 2;
        enc2 = ((chr1 & 3) << 4) | (chr2 >> 4);
        enc3 = ((chr2 & 15) << 2) | (chr3 >> 6);
        enc4 = chr3 & 63;

        if (isNaN(chr2)) {
            enc3 = enc4 = 64;
        } else if (isNaN(chr3)) {
            enc4 = 64;
        }

        output = output +
    keyStr.charAt(enc1) +
    keyStr.charAt(enc2) +
    keyStr.charAt(enc3) +
    keyStr.charAt(enc4);
        chr1 = chr2 = chr3 = "";
        enc1 = enc2 = enc3 = enc4 = "";
    } while (i < input.length);

    return output;
}

function decode64(input) {
    var output = "";
    var chr1, chr2, chr3 = "";
    var enc1, enc2, enc3, enc4 = "";
    var i = 0;

    // remove all characters that are not A-Z, a-z, 0-9, +, /, or =
    var base64test = /[^A-Za-z0-9\+\/\=]/g;
    if (base64test.exec(input)) {
        alert("There were invalid base64 characters in the input text.\n" +
        "Valid base64 characters are A-Z, a-z, 0-9, '+', '/',and '='\n" +
        "Expect errors in decoding.");
    }
    input = input.replace(/[^A-Za-z0-9\+\/\=]/g, "");

    do {
        enc1 = keyStr.indexOf(input.charAt(i++));
        enc2 = keyStr.indexOf(input.charAt(i++));
        enc3 = keyStr.indexOf(input.charAt(i++));
        enc4 = keyStr.indexOf(input.charAt(i++));

        chr1 = (enc1 << 2) | (enc2 >> 4);
        chr2 = ((enc2 & 15) << 4) | (enc3 >> 2);
        chr3 = ((enc3 & 3) << 6) | enc4;

        output = output + String.fromCharCode(chr1);

        if (enc3 != 64) {
            output = output + String.fromCharCode(chr2);
        }
        if (enc4 != 64) {
            output = output + String.fromCharCode(chr3);
        }

        chr1 = chr2 = chr3 = "";
        enc1 = enc2 = enc3 = enc4 = "";

    } while (i < input.length);

    return unescape(output);
}

//propertyOrder ==> array of property names in order of sort precedence
//****NOTE if a property is an array it is sorted by length
//*****Overload if an Array property starts with v_ then it is sorted by the concatenated values of the array, weird, but functional, mostly to provide a simple consistent sort order
//desc ==> bool true == decending order
function JsonSort(a, b, propertyOrder, desc) {
    if (propertyOrder.constructor !== Array) { throw new Error("propertyOrder must be an array"); }
    var newA = a;
    var newB = b;
    if (desc) { //switch comparison order
        newA = b;
        newB = a;
    }
    for (var i = 0; i < propertyOrder.length; i++) {
        var property = propertyOrder[i];
        var withValues = false;
        if (propertyOrder[i].indexOf("v_") === 0) {
            property = propertyOrder[i].substring(2);
            withValues = true;
        }
        var compA = getCompareValue(newA[property], withValues);
        var compB = getCompareValue(newB[property], withValues); 
        if (compB < compA) {
                return 1;
        } 
        if (compB > compA) {
                return -1;
        }
    }
    return 0;

    function getCompareValue(value, withValues) {
        if (value.constructor === Array) {
            var tempValue = '000' + value.length;
            tempValue = tempValue.substring(tempValue.length - 4);
            if (withValues) {
                tempValue += value.join(',');
            }
            value = tempValue;
        }
        return value;
    }
}


insertScriptTag('.\/scripts\/ApnxHandshake.js');

