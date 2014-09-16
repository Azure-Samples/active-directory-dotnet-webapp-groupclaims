window.AadPicker = function (endpoint, numRes, callbck) {
    function AadPicker() {
        this.numResults = numRes;
        this.callback = callbck;
        this.host = endpoint;
        this.selected = null;
    }

    AadPicker.prototype.Search = function (inputValue) {

        var url = this.host + "?input=" + inputValue + "&quantity=" + this.numResults;

        $.ajax({
            url: url,
            picker: this,
            beforeSend: function (jqxhr, settings) {
                jqxhr.overrideMimeType("application/json");
            },
        }).done(function (data, textStatus, jqxhr) {
            console.log(data);
            var results = { error: "Error during searching" };
            if (data.error == undefined) {
                var groupResults = [];
                var userResults = [];
                var j = 0;

                for (; j < data.groups.value.length && j < parseInt(data.numResults) / 2; j++)
                    groupResults.push(data.groups.value[j]);

                for (var i = 0; i < data.users.value.length && groupResults.length + userResults.length < parseInt(data.numResults) ; i++)
                    userResults.push(data.users.value[i]);

                for (; j < data.groups.value.length && groupResults.length + userResults.length < parseInt(data.numResults) ; j++)
                    groupResults.push(data.groups.value[j]);

                var results = groupResults.concat(userResults);
            }

            this.picker.callback.call(this.picker, results);
        })
        .fail(function (jqxhr, textStatus, errorThrown) {
            alert(textStatus);
        })
    };
    return new AadPicker();
};




