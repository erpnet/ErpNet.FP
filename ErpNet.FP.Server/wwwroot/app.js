var availablePrinters = {}

function showAvailablePrinters() {
    $.ajax({
        type: 'GET',
        url: '/printers',
        data: {},
        dataType: 'json',
        timeout: 0,
        context: $('#PrintersList'),
        success: function (data) {
            availablePrinters = data
            this.html("")
            for (var printerId in data) {
                var printer = data[printerId]
                var url = window.location.protocol +
                    "//" +
                    window.location.host +
                    "/printers/" +
                    printerId
                var characteristics =
                    '<li><strong><a target="blank_" href="' + url + '">' + url + '</a></strong></li>'
                for (var characteristic in printer) {
                    characteristics += '<li>' + characteristic + ':&nbsp;<strong>' + printer[characteristic] + '</strong></li>'
                }
                var section =
                    '<input type="radio" id="available-section-' + printerId + '" aria-hidden="true" name="available">' +
                    '<label style="overflow:hidden;display:inline-block;text-overflow: ellipsis;white-space: nowrap;" for="available-section-' + printerId + '" aria-hidden="true"><strong>' + printerId + '</strong></label>' +
                    '<div><ul>' + characteristics + '</ul>' +
                    '<button class="small primary" onclick="printZReport(\'' + printerId + '\')">Z-Report</button>' +
                    '<button class="small primary" onclick="printXReport(\'' + printerId + '\')">X-Report</button>' +
                    '<button class="small primary" onclick="resetPrinter(\'' + printerId + '\')">Reset</button>' +
                    '</div>'
                this.append(section)
            }
            var printersCount = Object.keys(data).length
            this.append('<p>Available ' + printersCount + ' printer(s).</p>')

            showConfiguredPrinters()
        },
        error: function (xhr, type) {
            showToastMessage("Cannot get available printers list.")
            this.html("")
            $('#ConfiguredPrintersList').html("")
        }
    })
}

function autoDetectChanged() {
    $.ajax({
        type: 'GET',
        url: '/service/toggleautodetect',
        data: {},
        dataType: 'json',
        timeout: 0,
        success: function (data) {
            $("#Version").html('ver.' + data.version)
            $("#ServerId").html('Server Id: ' + data.serverId)
            $("#AutoDetect").attr('checked', data.autoDetect ? 'checked' : null)
            showToastMessage("Auto detect is " + (data.autoDetect ? 'turned ON' : 'turned OFF'))
        },
        error: function (xhr, type) {
            showToastMessage("Cannot change auto detect mode.")
        }
    })
}

function showConfiguredPrinters() {
    $.ajax({
        type: 'GET',
        url: '/service/printers',
        data: {},
        dataType: 'json',
        timeout: 0,
        context: $('#ConfiguredPrintersList'),
        success: function (data) {
            this.html("")
            for (var printerId in data) {
                var printer = data[printerId]
                var section =
                    '<input type="radio" id="configured-section-' + printerId + '" aria-hidden="true" name="configured">' +
                    '<label style="overflow:hidden;display:inline-block;text-overflow: ellipsis;white-space: nowrap;" for="configured-section-' + printerId + '" aria-hidden="true"><strong>' + printerId + '</strong></label>' +
                    '<div class="input-group vertical">' +
                    '<label for="id-' + printerId + '">Id:</label><input id="id-' + printerId + '" value="' + printerId + '" />' +
                    '<label for="uri-' + printerId + '">Uri:</label><input id="uri-' + printerId + '" value="' + printer.uri + '" />' +
                    '<div class="input-group horizontal">' +
                    '<button class="small primary" onclick="saveSettingsForPrinter(\'' + printerId + '\')">Save settings</button>' +
                    '<button class="small secondary" onclick="deletePrinter(\'' + printerId + '\', \'' + printer.uri + '\')">Delete printer</button>' +
                    '</div></div>'
                this.append(section)
            }
            var printersCount = Object.keys(data).length
            this.append('<p>Configured ' + printersCount + ' printer(s).</p>')
        },
        error: function (xhr, type) {
            showToastMessage("Cannot get configured printers list.")
            this.html("")
        }
    })
}

function getServerVariables() {
    $.ajax({
        type: 'GET',
        url: '/service/vars',
        data: {},
        dataType: 'json',
        timeout: 0,
        success: function (data) {
            console.log(data)
            $("#Version").html('ver.' + data.version)
            $("#ServerId").html('Server Id: ' + data.serverId)
            $("#AutoDetect").attr('checked', data.autoDetect ? 'checked' : null)
        },
        error: function (xhr, type) {
            showToastMessage("Cannot get server variables.")
        }
    })
}

function detectAvailablePrinters() {
    availablePrinters = {}
    $("#DetectButton").attr("disabled", "disabled")
    $('#PrintersList').html('<div class="spinner primary"></div>Detecting...')
    $('#ConfiguredPrintersList').html('<div class="spinner primary"></div>Loading...')
    $.ajax({
        type: 'GET',
        url: '/service/detect',
        data: {},
        timeout: 0,
        success: function (data) {
            showPrinters()
            $("#DetectButton").attr("disabled", null)
        },
        error: function (xhr, type) {
            showToastMessage("Cannot detect printers now. Try again later.")
            showPrinters()
            $("#DetectButton").attr("disabled", null)
        }
    })
}

function showPrinters() {
    showAvailablePrinters()
}

function configurePrinter() {
    $("#NewPrinterModal").prop('checked', false)
    $.ajax({
        type: 'POST',
        url: '/service/printers/configure',
        data: JSON.stringify({
            "id": $("#NewPrinterId").val(),
            "uri": $("#NewPrinterUri").val()
        }),
        contentType: 'application/json',
        dataType: 'json',
        timeout: 0,
        success: function (data) {
            detectAvailablePrinters()
        },
        error: function (xhr, type) {
            showToastMessage("Cannot configure the new printer.")
        }
    })
}

function deletePrinter(printerId, printerUri) {
    $.ajax({
        type: 'POST',
        url: '/service/printers/delete',
        data: JSON.stringify({
            "id": printerId,
            "uri": printerUri
        }),
        contentType: 'application/json',
        dataType: 'json',
        timeout: 0,
        success: function (data) {
            detectAvailablePrinters()
        },
        error: function (xhr, type) {
            showToastMessage("Cannot delete the printer configuration.")
        }
    })
}

function saveSettingsForPrinter(printerId) {
    $.ajax({
        type: 'POST',
        url: '/service/printers/delete',
        data: JSON.stringify({
            "id": printerId
        }),
        contentType: 'application/json',
        dataType: 'json',
        timeout: 0,
        success: function (data) {
            $.ajax({
                type: 'POST',
                url: '/service/printers/configure',
                data: JSON.stringify({
                    "id": $("#id-" + printerId).val(),
                    "uri": $("#uri-" + printerId).val(),
                }),
                contentType: 'application/json',
                dataType: 'json',
                timeout: 0,
                success: function (data) {
                    detectAvailablePrinters()
                },
                error: function (xhr, type) {
                    showToastMessage("Cannot save the changes for the printer.")
                }
            })
        },
        error: function (xhr, type) {
            showToastMessage("Cannot delete the old settings.")
        }
    })
}

function printZReport(printerId) {
    $.ajax({
        type: 'POST',
        url: '/printers/' + printerId + '/zreport',
        data: {},
        contentType: 'application/json',
        dataType: 'json',
        timeout: 0,
        success: function (data) {
            if (data.ok) {
                showToastMessage("The Z-Report printing is done.")
            } else {
                var errors = "";
                for (var ix in data.messages) {
                    var message = data.messages[ix]
                    if (message.type == "error") {
                        errors += message.text + "; "
                    }
                }
                showToastMessage("Cannot print the Z-Report: " + errors.trim())
            }
        },
        error: function (xhr, type) {
            showToastMessage("Cannot print the Z-Report.")
        }
    })
}

function printXReport(printerId) {
    $.ajax({
        type: 'POST',
        url: '/printers/' + printerId + '/xreport',
        data: {},
        contentType: 'application/json',
        dataType: 'json',
        timeout: 0,
        success: function (data) {
            if (data.ok) {
                showToastMessage("The X-Report printing is done.")
            } else {
                var errors = "";
                for (var ix in data.messages) {
                    var message = data.messages[ix]
                    if (message.type == "error") {
                        errors += message.text + "; "
                    }
                }
                showToastMessage("Cannot print the X-Report: " + errors.trim())
            }
        },
        error: function (xhr, type) {
            showToastMessage("Cannot print the X-Report.")
        }
    })
}

function resetPrinter(printerId) {
    $.ajax({
        type: 'POST',
        url: '/printers/' + printerId + '/reset',
        data: {},
        contentType: 'application/json',
        dataType: 'json',
        timeout: 0,
        success: function (data) {
            if (data.ok) {
                showToastMessage("Printer is reset.")
            } else {
                var errors = "";
                for (var ix in data.messages) {
                    var message = data.messages[ix]
                    if (message.type == "error") {
                        errors += message.text + "; "
                    }
                }
                showToastMessage("Cannot reset the printer: " + errors.trim())
            }
        },
        error: function (xhr, type) {
            showToastMessage("Cannot reset the printer.")
        }
    })
}


function showToastMessage(msg) {
    var toastArea = $("#ToastArea")
    toastArea.html('<span class="toast">' + msg + '</span>')
    setTimeout(function () { toastArea.html(""); }, 3000);
}

$(function () {
    getServerVariables()
    showPrinters()
})
