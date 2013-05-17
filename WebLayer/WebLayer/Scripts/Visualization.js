function isChildNode(selectedNodeIndex, testingNodeIndex) {
    return isChildNodeByNode(nodes[selectedNodeIndex], nodes[testingNodeIndex]);
}

function isChildNodeByNode(selectedNode, testingNode) { //child, parent
    var selectedNodeMeasures = selectedNode.MeasureSet;
    var testingNodeMeasures = testingNode.MeasureSet;
    if (selectedNodeMeasures.length > testingNodeMeasures.length) { return false; }
    var isPresent = false;
    for (var measure in selectedNodeMeasures) {
        isPresent = false;
        for (var compareMeasure in testingNodeMeasures) {
            if (selectedNodeMeasures[measure] == testingNodeMeasures[compareMeasure]) { isPresent = true; break; }
        }
        if (!isPresent) { return false };
    }
    return true;
}

function sortByValuations(a, b) { return (b.MaxValuation + b.MeasureSet.length / 10000) - (a.MaxValuation + a.MeasureSet.length / 10000); } //first by value then by tier

function sortByTier(a, b) { return (b.MeasureSet.length + b.MaxValuation / 1000) - (a.MeasureSet.length + a.MaxValuation / 1000); } //first by tier then by value

function ShowValuationGraph(valuations, sortFunction, colorFunction) {
    valuations = valuations.sort(sortFunction); // get the list sorted ASAP
    var ctx = this.dataCtx;
    ctx.fillStyle = 'aaaaaa';
    ctx.fillRect(0, 0, this.dataCanvas.width, this.dataCanvas.height);
    this.DrawGraph(ctx, colorFunction, valuations, true);
}

function ShowGraphHighlights(valuations, sortFunction, colorFunction) {
    valuations = valuations.sort(sortFunction); // get the list sorted ASAP
    var ctx = this.highlightCtx;
    ctx.clearRect(0, 0, this.highlightCanvas.width, this.highlightCanvas.height);
    this.DrawGraph(ctx, colorFunction, valuations, false);
}

//TODO need to refactor showlineage to use DrawGraph
function ShowLineage(valuations, sortFunction, colorFunction, selectedIndex) {
    valuations = valuations.sort(sortFunction); // get the list sorted ASAP
    var selectedNode = valuations[selectedIndex];
    var totalNodes = valuations.length;
    var canvasWidth = this.clickCanvas.width;
    var canvasHeight = this.clickCanvas.height;
    var dims = this.GetCellDimensions(totalNodes);

    var ctx = this.clickCtx;
    ctx.lineWidth = 2;
    ctx.clearRect(0, 0, canvasWidth, canvasHeight);

    var nodesRendered = 0;
    for (var j = 1; j <= dims.maxHorizontalNodes; j++) {
        var currentNode, nodeType, TopLeft;
        for (var i = 0; i < dims.maxVerticalNodes; i++) {
            nodesRendered++;
            if (nodesRendered > totalNodes) {
                break;
            }
            currentNode = valuations[nodesRendered - 1];
            nodeType = '';
            TopLeft = { x: canvasWidth - (dims.width * j), y: i * dims.height };
            if (currentNode.MeasureSet.join(',') == selectedNode.MeasureSet.join(',')) {
                nodeType = 'self';
            }
            else {
                if (isChildNodeByNode(selectedNode, currentNode)) {
                    nodeType = 'parent';
                }
                else {
                    if (isChildNodeByNode(currentNode, selectedNode)) {
                        nodeType = 'child';
                    }
                }
            }
            if (nodeType != '') {
                ctx.strokeStyle = colorFunction(nodeType)
                ctx.strokeRect(TopLeft.x + 1, TopLeft.y, dims.width - 1, dims.height - 1);
            }
        }  //vertical
    } //horizontal 
}

function getCellDimensions(totalNodes) {
    var canvasWidth = this.dataCanvas.width;
    var canvasHeight = this.dataCanvas.height;
    var maxDimension = Math.floor(.51 * canvasHeight);
    var width, height, maxVerticalNodes, maxHorizontalNodes;
    var adjustment = 0;
    do {
        width = Math.ceil(Math.min(Math.sqrt(canvasWidth * canvasHeight / totalNodes), maxDimension)) - adjustment;
        height = width;
        maxVerticalNodes = Math.floor(canvasHeight / height);
        maxHorizontalNodes = Math.ceil(totalNodes / maxVerticalNodes);
        adjustment++;
    }
    while ((width * maxHorizontalNodes > canvasWidth) || (height * maxVerticalNodes > canvasHeight) || (maxHorizontalNodes * maxVerticalNodes < totalNodes));
    this.cellDimensions = { 'width': width,
        'height': height,
        'maxVerticalNodes': maxVerticalNodes,
        'maxHorizontalNodes': maxHorizontalNodes
    };
    return this.cellDimensions;
}

function drawGraph(canvasContext, getColor, nodeList, isFilled) {
    var dims = this.GetCellDimensions(nodeList.length);
    var totalNodes = nodeList.length;
    var nodesRendered = 0;
    for (var j = 1; j <= dims.maxHorizontalNodes; j++) {
        for (var i = 0; i < dims.maxVerticalNodes; i++) {
            nodesRendered++;
            if (nodesRendered > totalNodes) { //needs to be done as final column may not be fully filled
                break;
            }
            var currentNode = nodeList[nodesRendered - 1];
            var TopLeft = { x: this.dataCanvas.width - (dims.width * j), y: i * dims.height };
            if (isFilled){
                canvasContext.fillStyle = getColor(currentNode);
                canvasContext.fillRect(TopLeft.x + 1, TopLeft.y, dims.width - 1, dims.height - 1); //fill one pixel smaller, lets the canvas color show through
            }
            else {
                canvasContext.strokeStyle = getColor(currentNode);
                canvasContext.strokeRect(TopLeft.x + 1, TopLeft.y, dims.width - 1, dims.height - 1);
            }
        } //vertical
    } //horizontal 
}

function visualizer(dataCanvas, highlightCanvas, clickCanvas, clickHandler, height, width) {
    //body of constructor
    this.dataCanvas = dataCanvas;
    this.dataCanvas.height = height;
    this.dataCanvas.width = width;
    this.highlightCanvas = highlightCanvas;
    this.highlightCanvas.height = height;
    this.highlightCanvas.width = width;
    this.clickCanvas = clickCanvas;
    this.clickCanvas.height = height;
    this.clickCanvas.width = width;
    this.dataCtx = dataCanvas.getContext('2d');
    this.highlightCtx = highlightCanvas.getContext('2d');
    this.clickCtx = clickCanvas.getContext('2d');
    this.clickCanvas.onmousedown = mousey;
    this.cellDimensions = null;
    var that = this;

    function getMousePos(targ, e) {
        if (!e)
            e = window.event;
        if (e.target)
            targ = e.target;
        else if (e.srcElement)
            targ = e.srcElement;
        if (targ.nodeType == 3) // defeat Safari bug
            targ = targ.parentNode;

        // jQuery normalizes the pageX and pageY
        // pageX,Y are the mouse positions relative to the document
        // offset() returns the position of the element relative to the document
        var x = e.pageX - $(targ).offset().left;
        var y = e.pageY - $(targ).offset().top;

        return { "x": x, "y": y };
    }

    function mousey(evt) {
        var mousePos = getMousePos(that.clickCanvas, evt);
        var col = Math.floor((that.clickCanvas.width - mousePos.x) / that.cellDimensions.width);
        var row = Math.floor(mousePos.y / that.cellDimensions.height);
        var index = col * that.cellDimensions.maxVerticalNodes + row;
        clickHandler(index);
    }
}

visualizer.prototype.ShowValuationGraph = ShowValuationGraph;
visualizer.prototype.ShowGraphHighlights = ShowGraphHighlights;
visualizer.prototype.GetCellDimensions = getCellDimensions;
visualizer.prototype.ShowLineage = ShowLineage; 
visualizer.prototype.DrawGraph = drawGraph;