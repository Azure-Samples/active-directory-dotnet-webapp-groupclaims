window.AadPicker = function (searchUrl, maxResultsPerPage, input, token, tenant) {
    function AadPicker() {
        // Inputs
        this.resultsPerPage = maxResultsPerPage / 2;
        this.$input = $( input );
        this.searchUrl = searchUrl;
        this.token = token;
        this.tenant = tenant;

        // Outputs
        this.selected = null;

        // Members
        this.currentResults = {};
        this.userSkipToken = null;
        this.groupSkipToken = null;
        this.lastDisplayed = "";
        //this.lastInput;
        this.isPaging = false;
        this.selectColor;
        this.unSelectColor;
        this.hoverColor;

        // Constants
        this.graphLoc = "https://graph.windows.net";
        this.apiVersion = "1.22-preview";

        // UI Labels
        this.userLabel = "";
        this.groupLabel = "(group)";

        // Activate
        //this.SearchBoth("");
        this.Listen();

        // Get CSS Properties for Dynamic Color Changes
        var $temp1 = $("<div></div>").addClass("aadpicker-result-selected").css("display", "none");
        var $temp2 = $("<div></div>").addClass("aadpicker-result-elem").css("display", "none");
        var $temp3 = $("<div></div>").addClass("aadpicker-result-hovered").css("display", "none");
        $temp1.appendTo("body");
        $temp2.appendTo("body");
        $temp3.appendTo("body");
        this.selectColor = $temp1.css("background-color");
        this.unSelectColor = $temp2.css("background-color");
        this.hoverColor = $temp3.css("background-color");
        $temp1.detach();
        $temp2.detach();
        $temp3.detach();
    }

    AadPicker.prototype.Listen = function () {
        var picker = this;
        this.$input.catcomplete({
            source: function (request, response) {
                picker.Search(request.term, response);
            },
            minLength: 0,
            delay: 500,
            open: function (event, ui) {
                //picker.Select();
            },
            select: function (event, ui) {
                event.preventDefault();
            },
            focus: function (event, ui) {
                event.preventDefault();
            },
        }).data("custom-catcomplete").close = function (event) {
            return false;
        }
    }

    AadPicker.prototype.ConstructUserQuery = function (inputValue) {
        
        var url = this.graphLoc + '/' + this.tenant + "/users?api-version="
            + this.apiVersion + "&$top=" + this.resultsPerPage;

        if (inputValue.length > 0) {
            url += "&$filter=" +
            "startswith(displayName,'" + inputValue +
            "') or startswith(givenName,'" + inputValue +
            "') or startswith(surname,'" + inputValue +
            "') or startswith(userPrincipalName,'" + inputValue +
            "') or startswith(mail,'" + inputValue +
            "') or startswith(mailNickname,'" + inputValue +
            "') or startswith(jobTitle,'" + inputValue +
            "') or startswith(department,'" + inputValue +
            "') or startswith(city,'" + inputValue + "')";
        }

        if (this.userSkipToken && inputValue == this.lastDisplayed)
            url += '&' + this.userSkipToken;

        return url;
    }

    AadPicker.prototype.ConstructGroupQuery = function (inputValue) {

        var url = this.graphLoc + '/' + this.tenant + "/groups?api-version="
            + this.apiVersion + "&$top=" + this.resultsPerPage;

        if (inputValue.length > 0) {
            url += "&$filter=" +
            "startswith(displayName,'" + inputValue +
            "') or startswith(mail,'" + inputValue +
            "') or startswith(mailNickname,'" + inputValue + "')";
        }

        if (this.groupSkipToken && inputValue == this.lastDisplayed)
            url += '&' + this.groupSkipToken;

        return url;
    }

    AadPicker.prototype.SendQuery = function (graphQuery) {
        
        return $.ajax({
            url: this.searchUrl,
            type: "POST",
            picker: this,
            data: {
                token: this.token,
                query: graphQuery
            },
            beforeSend: function (jqxhr, settings) {
                jqxhr.overrideMimeType("application/json");
            }
        });
    }

    AadPicker.prototype.Search = function (inputValue, callback) {

        var userQuery = this.ConstructUserQuery(inputValue);
        var groupQuery = this.ConstructGroupQuery(inputValue);
                
        var userDeffered = this.SendQuery(userQuery);
        var groupDeffered = this.SendQuery(groupQuery);

        var recordResults = function (picker, inputValue, callback) {
            return function (userQ, groupQ) {
                var results;

                if (userQ[1] == "success" && groupQ[1] == "success" 
                    && userQ[0].error == undefined && groupQ[0].error == undefined) {

                    results = [];
                    var usersAndGroups = userQ[0].value.concat(groupQ[0].value);

                    if (userQ[0]["odata.nextLink"] != undefined) {
                        picker.userSkipToken = userQ[0]["odata.nextLink"]
                            .substring(userQ[0]["odata.nextLink"].indexOf("$skiptoken"),
                            userQ[0]["odata.nextLink"].length);
                    }
                    else {
                        picker.userSkipToken = null;
                    }
                    if (groupQ[0]["odata.nextLink"] != undefined) {
                        picker.groupSkipToken = groupQ[0]["odata.nextLink"]
                            .substring(groupQ[0]["odata.nextLink"].indexOf("$skiptoken"),
                            groupQ[0]["odata.nextLink"].length);
                    }
                    else {
                        picker.groupSkipToken = null;
                    }

                    if (inputValue != picker.lastDisplayed)
                        picker.currentResults = {};

                    for (var i = 0; i < usersAndGroups.length; i++) {

                        if (usersAndGroups[i].objectType == "User") {
                            results.push({
                                label: usersAndGroups[i].displayName,
                                value: usersAndGroups[i].displayName,
                                objectId: usersAndGroups[i].objectId,
                                objectType: picker.userLabel,
                            });
                        } else if (usersAndGroups[i].objectType == "Group") {
                            results.push({
                                label: usersAndGroups[i].displayName,
                                value: usersAndGroups[i].displayName,
                                objectId: usersAndGroups[i].objectId,
                                objectType: picker.groupLabel,
                            });
                        }
                        

                        picker.currentResults[usersAndGroups[i].objectId] = {
                            objectId: usersAndGroups[i].objectId,
                            displayName: usersAndGroups[i].displayName,
                            objectType: usersAndGroups[i].objectType
                        };
                    }
                }
                else {
                    picker.currentResults = {};
                    results = [{ label: "Error During Search" }];
                }
                
                picker.selected = null; //TODO
                callback(results);
            };
        };

        $.when(userDeffered, groupDeffered)
            .always(recordResults(this, inputValue, callback))
    };

    $.widget("custom.catcomplete", $.ui.autocomplete, {
        _create: function () {
            this._super();
            this.widget().menu("option", "items", "> :not(.ui-autocomplete-category)");
        },
        _renderMenu: function (ul, items) {
            var that = this;

            $.each(items, function (index, item) {
                that._renderItemData(ul, item);
            });
        },
        _renderItem: function (ul, item) {
            var label = $("<div>").addClass("aadpicker-result-label").append(item.label);
            var type = $("<div>").addClass("aadpicker-result-type").append(item.objectType);
            var toappend = [label, type];

            return $("<li>").addClass("aadpicker-result-elem").attr("data-selected", "false")
                .attr("data-objectId", item.objectId).append(toappend).appendTo(ul);
        }
    });

    return new AadPicker();
};