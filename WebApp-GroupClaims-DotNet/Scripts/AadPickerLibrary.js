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
        this.lastInput;
        this.isPaging = false;
        this.selectColor;
        this.unSelectColor;
        this.hoverColor;

        // Constants
        this.graphLoc = "https://graph.windows.net";
        this.apiVersion = "1.22-preview";

        // Activate
        this.SearchBoth("");
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

        var timer = null;
        this.$input.on('input', { picker: this }, function (event) {
            var val = $(this).val();
            function runSearch(event, val) {
                event.data.picker.SearchBoth(val);
            }
            if (timer) {
                window.clearTimeout(timer);
            }
            timer = window.setTimeout(function () {
                runSearch(event, val);
            }, 200);
        });
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

    AadPicker.prototype.SearchBoth = function (inputValue) {

        this.lastInput = inputValue;

        if (inputValue == undefined)
            inputValue = "";

        var userQuery = this.ConstructUserQuery(inputValue);
        var groupQuery = this.ConstructGroupQuery(inputValue);
                
        var userDeffered = this.SendQuery(userQuery);
        var groupDeffered = this.SendQuery(groupQuery);

        var recordResults = function (picker, inputValue) {
            return function (userQ, groupQ) {
                var results = { error: "Error during searching" };

                if (userQ[1] == "success" && groupQ[1] == "success") {

                    if (userQ[0].error == undefined && groupQ[0].error == undefined)
                        results = groupQ[0].value.concat(userQ[0].value);

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
                    if (results.error == undefined) {
                        for (var i = 0; i < results.length; i++) {
                            picker.currentResults[results[i].objectId] = {
                                objectId: results[i].objectId,
                                displayName: results[i].displayName,
                                objectType: results[i].objectType
                            };
                        }
                    }

                }
                else {
                    picker.currentResults = {};
                }

                if (picker.lastInput == inputValue)
                    picker.Display(results, inputValue);

            };
        };

        $.when(userDeffered, groupDeffered)
            .always(recordResults(this, inputValue))
    };

    AadPicker.prototype.SearchUserOnly = function (inputValue) {

        this.lastInput = inputValue;

        var userQuery = this.ConstructUserQuery(inputValue);
        var userDeffered = this.SendQuery(userQuery);

        var recordResults = function (picker, inputValue) {
            return function (data, textStatus, jqxhr) {
                var results = { error: "Error during searching" };

                if (textStatus == "success") {

                    if (data.error == undefined)
                        results = data.value;

                    if (data["odata.nextLink"] != undefined) {
                        picker.userSkipToken = data["odata.nextLink"]
                            .substring(data["odata.nextLink"].indexOf("$skiptoken"),
                            data["odata.nextLink"].length);
                    }
                    else {
                        picker.userSkipToken = null;
                    }

                    if (inputValue != picker.lastDisplayed)
                        picker.currentResults = {};
                    if (results.error == undefined) {
                        for (var i = 0; i < results.length; i++) {
                            picker.currentResults[results[i].objectId] = {
                                objectId: results[i].objectId,
                                displayName: results[i].displayName,
                                objectType: results[i].objectType
                            };
                        }
                    }
                }
                else {
                    picker.currentResults = {};
                }
                if (picker.lastInput == inputValue)
                    picker.Display(results, inputValue);
            };

        };

        userDeffered.always(recordResults(this, inputValue))
    };

    AadPicker.prototype.SearchGroupOnly = function (inputValue) {

        this.lastInput = inputValue;

        var groupQuery = this.ConstructGroupQuery(inputValue);
        var groupDeffered = this.SendQuery(groupQuery);

        var recordResults = function (picker, inputValue) {
            return function (data, textStatus, jqxhr) {
                var results = { error: "Error during searching" }; 

                if (textStatus == "success") {

                    if (data.error == undefined)
                        results = data.value;

                    if (data["odata.nextLink"] != undefined) {
                        picker.groupSkipToken = data["odata.nextLink"]
                            .substring(data["odata.nextLink"].indexOf("$skiptoken"),
                            data["odata.nextLink"].length);
                    }
                    else {
                        picker.groupSkipToken = null;
                    }

                    if (inputValue != picker.lastDisplayed)
                        picker.currentResults = {};
                    if (results.error == undefined) {
                        for (var i = 0; i < results.length; i++) {
                            picker.currentResults[results[i].objectId] = {
                                objectId: results[i].objectId,
                                displayName: results[i].displayName,
                                objectType: results[i].objectType
                            };
                        }
                    }
                }
                else {
                    picker.currentResults = {};
                }
                
                if (picker.lastInput == inputValue)
                    picker.Display(results, inputValue);
            };

        };

        groupDeffered.always(recordResults(this, inputValue));
    };

    AadPicker.prototype.SelectHandler = function () {
        if ($(this).attr("data-selected") == "true") {
            $(this).attr("data-selected", "false");
            $(this).css("background-color", picker.unSelectColor);
            picker.selected = null;
            picker.$input.get()[0].value = picker.lastDisplayed;
        }
        else if ($(this).attr("data-selected") == "false") {
            $(".aadpicker-result-elem").attr("data-selected", "false").css("background-color", picker.unSelectColor);
            $(this).attr("data-selected", "true");
            $(this).css("background-color", picker.selectColor);
            picker.selected = picker.currentResults[$(this).attr("data-objectId")];
            picker.$input.get()[0].value = $(this).text();
        }
    };

    AadPicker.prototype.MouseEnterHandler = function () {
        if ($(this).attr("data-selected") == "false")
            $(this).css("background-color", picker.hoverColor);
    };

    AadPicker.prototype.MouseLeaveHandler = function () {
        if ($(this).attr("data-selected") == "false")
            $(this).css("background-color", picker.unSelectColor);
    };

    AadPicker.prototype.ScrollHandler = function () {

        if ($(this).scrollTop() + $(this).innerHeight() >= this.scrollHeight && !picker.isPaging)
            picker.Page();
    };

    AadPicker.prototype.Page = function () {
        if (this.userSkipToken && this.groupSkipToken) {
            this.SearchBoth(this.lastDisplayed);
            this.isPaging = true;
        }
        else if (this.userSkipToken) {
            this.SearchUserOnly(this.lastDisplayed);
            this.isPaging = true;
        }
        else if (this.groupSkipToken) {
            this.SearchGroupOnly(this.lastDisplayed);
            this.isPaging = true;
        }
    };

    AadPicker.prototype.Select = function ($resultsDiv) {

        $resultsDiv.off("click", ".aadpicker-result-elem", this.SelectHandler)
            .on("click", ".aadpicker-result-elem", { picker: this }, this.SelectHandler);

        $resultsDiv.off("mouseenter", ".aadpicker-result-elem", this.MouseEnterHandler)
            .off("mouseleave", ".aadpicker-result-elem", this.MouseLeaveHandler)
            .on("mouseenter", ".aadpicker-result-elem", { picker: this }, this.MouseEnterHandler)
            .on("mouseleave", ".aadpicker-result-elem", { picker: this }, this.MouseLeaveHandler);
    };

    AadPicker.prototype.Paging = function ($resultsDiv) {

        this.isPaging = false;

        $resultsDiv.unbind("scroll", this.ScrollHandler)
            .bind("scroll", { picker: this }, this.ScrollHandler);

        if ($resultsDiv.get(0).offsetHeight >= $resultsDiv.get(0).scrollHeight)
            this.Page();
    };

    AadPicker.prototype.Display = function (results, inputValue) {
        
        var $resultsDiv = $(".aadpicker-search-results");
        if ($resultsDiv.length == 0) {
            $resultsDiv = $("<div></div>").addClass("aadpicker-search-results");
            this.$input.after($resultsDiv);
        }

        if (results.error != undefined) {
            var $msg = $("<p>" + results.error + "</p>").css("color", "red");
            this.lastDisplayed = results.error;
            $resultsDiv.empty();
            $resultsDiv.append($msg);
            return;
        }

        var toAppend = [];
        for (var i = 0; i < results.length; i++) {
            var $elem;
            if (results[i].objectType == "Group") {
                $elem = $("<div>" + results[i].displayName + " (Security Group)</div>").addClass("aadpicker-result-elem").attr("data-selected", "false").attr("data-objectid", results[i].objectId);
            }
            else if (results[i].objectType == "User") {
                $elem = $("<div>" + results[i].displayName + " (" + results[i].userPrincipalName + ")</div>").addClass("aadpicker-result-elem").attr("data-selected", "false").attr("data-objectid", results[i].objectId);
            }

            toAppend.push($elem);
        }

        if (inputValue != this.lastDisplayed || this.lastDisplayed == "Error during searching" || !this.isPaging)
            $resultsDiv.empty();
        $resultsDiv.append(toAppend);

        this.lastDisplayed = inputValue;

        this.Select($resultsDiv);

        this.Paging($resultsDiv);
    }


    return new AadPicker();
};