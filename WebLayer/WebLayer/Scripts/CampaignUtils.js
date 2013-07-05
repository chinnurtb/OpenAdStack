function CampaignClient(company, campaign, campaginSchemaDictionary) {
    //assumes global reference to JSONPath
    var Now = new Date();
    var campaignObject = { ExternalName: 'New Campaign', Properties: { Budget: 0.00, CPM: 0.00, StartDate: Now.toISOString(), EndDate: Now.toISOString(), Status: 'Draft', InventoryStrategy: 2} }

    if (company != null && campaign != null) {
        campaignObject = getCachedCampaign(company, campaign);
    }

    var isDirty = false;
    this.Save = function () { saveCampaign(); return campaignObject };
    this.Refresh = function () { getCampaignFromServerAndUpdateCache(company, campaign); campaignObject = getCachedCampaign(company, campaign); return true; };
    this.Campaign = function () { return campaignObject; };
    this.Set = function (key, value) { return trySetProperty(key, value); };
    this.Get = function (key) { return tryGetProperty(key); };

    //TODO be more robust around failures
    function saveCampaign() {
        var verb = "PUT";
        var url = $RCAPI.URI.CampaignUpdate.format(company, campaign);
        if (tryGetProperty("Id") == undefined) {
            verb = "POST";
            url = $RCAPI.URI.CampaignCreate.format(company);
        }
        var campaignAjaxResponse = new $RCAjax(url, JSON.stringify(campaignObject), verb, null, null, false, null, routeErrors);
        if (verb == "POST") {
            campaignObject = campaignAjaxResponse.responseData.Campaign;
        }
        else {
            getCampaignFromServerAndUpdateCache(company, campaign);
            campaignObject = getCachedCampaign(company, campaign);
        }
    }

    function tryGetProperty(key) {
        var schemaVersion = 0;
        var propertyValue = null;
        if (campaginSchemaDictionary[schemaVersion][key].IsPath) { //if schema key is a property or likely an association or requiries other JS to fetch the property
            propertyValue = jsonPath(campaignObject, '$.' + campaginSchemaDictionary[schemaVersion][key].value)
            propertyValue = propertyValue ? propertyValue[0] : null; 
        }
        else {
            propertyValue = campaginSchemaDictionary[schemaVersion][key].value(company, campaign, campaignObject);
        }
        while (propertyValue == null && schemaVersion < campaginSchemaDictionary.length - 1) {
            schemaVersion++;
            if (campaginSchemaDictionary[schemaVersion][key] != undefined) {
                if (jsonPath(campaignObject, '$.' + campaginSchemaDictionary[schemaVersion][key].IsPath)) {
                    propertyValue = jsonPath(campaignObject, '$.' + campaginSchemaDictionary[schemaVersion][key].value);
                    propertyValue = propertyValue ? propertyValue[0] : null;
                }
                else { // NOTE this migrates values from the older schema to a property on the new schema object.
                    // DO this to migrate old schema to new schema
                    //TODO this makes the object dirty every time, should be smarter about this
                    propertyValue = campaginSchemaDictionary[schemaVersion][key].value(company, campaign, campaignObject);
                    trySetProperty(key, propertyValue);
                    isDirty = true;
                }
            }
        }
        return propertyValue;
    }

    function trySetProperty(key, val) {
        if (campaginSchemaDictionary[0][key].value == undefined) {
            return false; //not known in the schema
        }
        isDirty = true;
        var path = campaginSchemaDictionary[0][key].value;
        var nodes = path.split('.');
// TODO make this support N-depth and encapsulate the functionality

        if (nodes.length >= 1 && campaignObject[nodes[0]] == undefined) {
            campaignObject[nodes[0]] = {};
        }
        if (nodes.length >= 2 && campaignObject[nodes[0]][nodes[1]] == undefined) {
            campaignObject[nodes[0]][nodes[1]] = {};
        }
        if (nodes.length >= 3 && campaignObject[nodes[0]][nodes[1]][nodes[2]] == undefined) {
            campaignObject[nodes[0]][nodes[1]][nodes[2]] = {};
        }
        if (nodes.length >= 4 && campaignObject[nodes[0]][nodes[1]][nodes[2]][nodes[3]] == undefined) {
            campaignObject[nodes[0]][nodes[1]][nodes[2]][nodes[3]] = {};
        }
        // TODO make this support N-depth and encapsulate the functionality

        switch (nodes.length) {
            case 1:
                campaignObject[nodes[0]] = val;
                break;
            case 2:
                campaignObject[nodes[0]][nodes[1]] = val;
                break;
            case 3:
                campaignObject[nodes[0]][nodes[1]][nodes[2]] = val;
                break;
            case 4:
                campaignObject[nodes[0]][nodes[1]][nodes[2]][nodes[3]] = val;
                break;
            default:
                return false;
        }
        return true;
    }

    function getCachedCampaign(companyId, campaignId) {
        var sessionCampaignKey = "currentCampaign";
        //Check if session has any campaign cached
        if (!sessionStorage.getItem(sessionCampaignKey)) {
            getCampaignFromServerAndUpdateCache(companyId, campaignId);
        }
        //Check to see if the one cached is old
        var now = new Date();
        var lastSaved = new Date(JSON.parse(sessionStorage.getItem(sessionCampaignKey)).lastSaved);
        if (now.getTime() - lastSaved.getTime() > .05 * 1000 * 60) { // 3 secs
            getCampaignFromServerAndUpdateCache(companyId, campaignId);
        }

        //Check to see if the cached item is the one we are looking for
        try {
            if (campaignId != JSON.parse(sessionStorage.getItem(sessionCampaignKey)).campaign.Campaign.ExternalEntityId) {
                getCampaignFromServerAndUpdateCache(companyId, campaignId);
            }
            return JSON.parse(sessionStorage.getItem(sessionCampaignKey)).campaign.Campaign;
        }
        catch (e) //if you fail to get a valid response
        {
            sessionStorage.removeItem(sessionCampaignKey);
            return null;
        }
    }

    function getCampaignFromServerAndUpdateCache(companyId, campaignId) {
        var campaign = campaignJSONCall("GET", false, $RCAPI.URI.CampaignGet.format(companyId, campaignId));
        sessionStorage.setItem("currentCampaign", JSON.stringify({
            "lastSaved": new Date(),
            "campaign": campaign
        }));
        isDirty = false;
        return campaign;
    }
}
////END of campaign client


var campaignSchema = [
{
    Budget: { IsPath: true, value: "Properties.Budget" },
    Name: { IsPath: true, value: "ExternalName" },
    Id: { IsPath: true, value: "ExternalEntityId" },
    Type: { IsPath: true, value: "ExternalType" },
    Version: { IsPath: true, value: "LocalVersion" },
    Status: { IsPath: true, value: "Properties.Status" },
    CPM: { IsPath: true, value: "Properties.CPM" },
    InventoryStrategy: { IsPath: true, value: "Properties.InventoryStrategy" },
    StartDate: { IsPath: true, value: "Properties.StartDate" },
    EndDate: { IsPath: true, value: "Properties.EndDate" },
    MeasureInfoSet: { IsPath: true, value: "Properties.MeasureInfoSet" },
    NodeValuationSet: { IsPath: true, value: "Properties.NodeValuationSet" },
    DAAllocationIndex: { IsPath: false, value: getDAAllocationIndex },
    DAApprovedVersion: { IsPath: true, value: "Properties.DAInputsApprovedVersion" }
},
{
    MeasureInfoSet: { IsPath: false, value: getEmptyMeasureInfoSet },
    NodeValuationSet: { IsPath: false, value: function (a, b, c) { return [] } }
}
];

function getEmptyMeasureInfoSet(advertiser, campaign, campaignObject) {
    return { IdealValuation: 0, MaxValuation: 0, Measures: [] };
}

function getDAAllocationIndex(advertiser, campaign, campaignObject){
    if (campaignObject.Associations == undefined || campaignObject.Associations.DAAllocationHistoryIndex == undefined) {
        return null;
    }
    var indexBlobId = campaignObject.Associations.DAAllocationHistoryIndex.TargetEntityId;
    var allocationIndex = campaignJSONCall("GET", false, $RCAPI.URI.CampaignGetBlob.format(advertiser, campaign, indexBlobId));
    return allocationIndex.Blob.sort(function(a,b){return a.AllocationStartTime > b.AllocationStartTime; });
}

function campaignJSONCall(messageType, async, url, data) {
    var companyAjax = new $RCAjax(url, data, messageType, null, null, async, null, routeErrors);
    return companyAjax.responseData;
}

function getCampaignList(companyId) {
    return campaignJSONCall("GET", false, $RCAPI.URI.CampaignCampaignForCompany.format(companyId));
}

function getCalculatedValuationList(companyId, campaignId) {
    return campaignJSONCall("GET", false, $RCAPI.URI.CampaignGetValuationPending.format(companyId, campaignId));
}

function routeErrors(data, errorType, errorText) {
    switch (data.status) {
        case 202:
            $RCUI.debugReport.record('accepted but not complete, please refresh the page');
            break;
        case 401:
            authExpiration();
            break;
        default:
            $RCUI.debugReport.record('Could not load campaign info: ' + errorType);
            break;
    };
}

function authExpiration() {
    alert("Not Authorized");
}

function getMeasureInfoSetFromCampaign(camp) {
    return camp.Get("MeasureInfoSet");
}

function getMeasuresFromCampaign(camp) {
    var MeasureInfoSet = getMeasureInfoSetFromCampaign(camp);
    return MeasureInfoSet.Measures;
}

function setMeasureInfoSet(camp, MeasureInfoSet) {
    return camp.Set("MeasureInfoSet", MeasureInfoSet);
}

//Make the grouped JSON for easier iteration for presentation
function getGroupedValuations() {
    var groupedMeasures = {};
    var groupName, escGroupName, topGroup;
    var MeasureInfoSet = getMeasuresFromCampaign(cClient);
    for (var i = 0; i < MeasureInfoSet.length; i++) {
        topGroup = MeasureInfoSet[i].pinned ? "Pinned" : "UnPinned";
        groupName = MeasureInfoSet[i].group == "" ? "_" : MeasureInfoSet[i].group;
        escGroupName = groupName.replace(' ', '_');

        if (groupedMeasures[topGroup] == undefined) {
            groupedMeasures[topGroup] = {};
        }

        if (groupedMeasures[topGroup][escGroupName] == undefined) {
            groupedMeasures[topGroup][escGroupName] = { Name: groupName, Measures: new Array };
        }

        groupedMeasures[topGroup][escGroupName].Measures.push({
            MeasureName: getMeasureDisplayName(MeasureInfoSet[i].measureId),
            MeasureId: MeasureInfoSet[i].measureId,
            Valuation: getMeasureValuation(MeasureInfoSet[i].measureId)
        });
    }
    return groupedMeasures;
}

//lookup display name for the measure
function getMeasureDisplayName(measureId) {
    var measureMapping = cachedMeasureMap.Get(measureId);
    if (measureMapping == undefined) {
        return "**Measure not found** (" + measureId + ")";
    }
    return measureMapping.displayName.replace(/:/g, ": ");
}

function getMeasureValuation(measureId) {
    var valuationList = getMeasuresFromCampaign(cClient);
    var valuation = 50;
    for (var i = 0; i < valuationList.length; i++) {
        if (valuationList[i].measureId == measureId) {
            valuation = valuationList[i].valuation;
            break;
        }
    }
    return valuation;
}

function getMeasureInfoSetDisplayName(measureList) {
    var displayValue = '';
    for (var i = 0; i < measureList.length; i++) {
        displayValue += getMeasureDisplayName(measureList[i]) + '; ';
    }
    return displayValue;
}

function getTierCount(measures) {
    var count = jsonPath(measures, '$.[?(@.group == "" && !@.pinned)]')? jsonPath(measures, '$.[?(@.group == "" && @.pinned == false)]').length:0;
    var groupedMeasures = jsonPath(measures, '$.[?(@.group != "" && @.group != undefined && @.pinned == false)]');
    var tempBucket = ',';
    for (var measure in groupedMeasures) {
        if (tempBucket.indexOf(',' + groupedMeasures[measure].group + ',') == -1) {
            count++;
            tempBucket = tempBucket + groupedMeasures[measure].group + ',';
        }
    }
    if (count == 0 && measures.length > 0) { count = 1;}
    return count;
}

function saveValuations(valuations) {
    cClient.Set("NodeValuationSetDraft", valuations);
    cClient.Save();
    workarea.progressOff();
}

var cClient;
//get domain data for measures
var measureArray;
var cachedMeasureMap;
function initializeCampaignClient() {
    if (getQueryString()['campaign'] != undefined) {
        cClient = new CampaignClient($RCUI.advertiser, $RCUI.campaign, campaignSchema);
        //get domain data for measures
        measureArray = jsonPath(getMeasuresFromCampaign(cClient), '$..measureId')
        var measuresAsString = measureArray ? measureArray.join(',') : ''
        cachedMeasureMap = new lazyDictionary('', 3000, measuresAsString);
    }
    else {
        cClient = new CampaignClient($RCUI.advertiser, null, campaignSchema)
    }
}

function getGroupNames(GroupedMeasures) {
    var GroupNames = [];
    for (var measure in GroupedMeasures) {
        var found = false;
        for (var group in GroupNames) {
            if (GroupedMeasures[measure].group == GroupNames[group]) {
                found = true;
                break;
            }
        }
        if (!found) {
            GroupNames.push(GroupedMeasures[measure].group);
        }
    }
    return GroupNames;
}

