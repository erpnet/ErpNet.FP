# The Net.FP Protocol
The ErpNet.FP print server accepts documents for printing, using the JSON based **Net.FP** - Net Fiscal Protocol.

# Contents

## Concepts
* [Printer Id](#printer-id)
* [Printer Uri](#printer-uri)

## Available Requests
* `GET` [Get The List Of Printers](#get-get-the-list-of-printers)
* `GET` [Get Printer Info](#get-get-printer-info)
* `GET` [Get Printer Status](#get-get-printer-status)
* `POST` [Print Fiscal Receipt](#post-print-fiscal-receipt)
* `POST` [Print Fiscal Receipt (With Operator Credentials)](#post-print-fiscal-receipt-with-operator-credentials)
* `POST` [Print Fiscal Receipt (Async)](#post-print-fiscal-receipt-async)
* `POST` [Print Fiscal Receipt (Idempotent)](#post-print-fiscal-receipt-idempotent)
* `GET` [Get Async Task Information](#get-get-async-task-information)
* `POST` [Print Reversal Receipt](#post-print-reversal-receipt)
* `POST` [Print Deposit Money Receipt](#post-print-deposit-money-receipt)
* `POST` [Print Withdraw Money Receipt](#post-print-withdraw-money-receipt)
* `POST` [Print X Report](#post-print-x-report)
* `POST` [Print Z Report](#post-print-z-report)
* `POST` [Set Printer Date And Time](#post-set-printer-date-and-time)
* `GET` [Get Current Cash Amount](#get-get-current-cash-amount)
* `POST` [Post Raw Request](#post-post-raw-request)
* `POST` [Print Last Receipt's Duplicate](#post-print-last-receipts-duplicate)

---

# Concepts

## Printer Id

Each printer is identified by its printerId. Although the library consumer does not need to know the exact specifics of the algorithm for generating the printerId, they are provided here for completeness:

1. For locally connected printers, the printerId is the printer serial number.
For example: "dy448967"

2. If a local printer is connected through multiple transports (for example: both COM and BT), the id for each next transport connection is assigned a consecutive number.
For example: "dy448967_1"
Note: The algorithm runs always in the same order. Unless transports are changed, the id would remain constant. However, it is advised to always use the default, non-numeric id.

3. For network printers, the printerId is provided in the configuration file.
For example: "FP_Room1"

## Printer Uri

When a printer is detected, the http server saves something, called printer Uri. The Uri contains the connection information to connect to the printer. It contains details about printer driver, transport and path to the printer. It is similar to the connection string pattern. Example Uris:
- bg.dy.isl.com://COM5
- bg.dy.isl.com://COM3
- bg.dt.x.isl.com://COM21
- bg.zk.zfp.http://fp5.mycompany.com
- bg.dt.p.isl.tcp://192.168.1.77:9100
- etc.

The printer Uri is currently used only internally in the http server. Still, it is exposed as part of the device info. In the future, there might be methods to use the printers through their Uri.

---

# Available requests

*Remark:* For all requests from now on, we will assume that, we are doing them on the local computer at: **http://localhost:8001**

Of course, this is not a must, and you can substitute the address and port, with yours, as well as the protocol, it can be both http and https.

## `GET` Get The List Of Printers
This request gets the list of available printers. The available printers are either auto-detected, or created by a config file.

### Example request:
```
http://localhost:8001/printers
```

### Example response:
```json
{
    "dt517985": {
        "uri": "bg.dt.c.isl.com://COM6",
        "serialNumber": "DT517985",
        "fiscalMemorySerialNumber": "02517985",
        "manufacturer": "Datecs",
        "model": "DP-25",
        "firmwareVersion": "263453 08Nov18 1312",
        "itemTextMaxLength": 22,
        "commentTextMaxLength": 40,
        "operatorPasswordMaxLength": 8,
        "taxIdentificationNumber": "000713391",
        "supportedPaymentTypes": [
            "cash",
            "coupons",
            "ext-coupons",
            "card",
            "reserved1"
        ]
    },
    "dt279013": {
        "uri": "bg.dt.p.isl.com://COM15",
        "serialNumber": "DT279013",
        "fiscalMemorySerialNumber": "02279013",
        "manufacturer": "Datecs",
        "model": "FP-2000",
        "firmwareVersion": "1.00BG 23NOV18 1000",
        "itemTextMaxLength": 36,
        "commentTextMaxLength": 42,
        "operatorPasswordMaxLength": 8,
        "taxIdentificationNumber": "000713391",
        "supportedPaymentTypes": [
            "cash",
            "check",
            "coupons",
            "ext-coupons",
            "packaging",
            "internal-usage",
            "damage",
            "card",
            "bank",
            "reserved1",
            "reserved2"
        ]
    },
    "is017220": {
        "uri": "bg.is.icp.com://COM3",
        "serialNumber": "IS017220",
        "fiscalMemorySerialNumber": "12022490",
        "manufacturer": "ISL",
        "model": "ISL5011S-KL",
        "firmwareVersion": "BG R1 21.01.201948",
        "itemTextMaxLength": 40,
        "commentTextMaxLength": 45,
        "operatorPasswordMaxLength": 0,
        "taxIdentificationNumber": "121108681",
        "supportedPaymentTypes": [
            "cash",
            "check",
            "coupons",
            "ext-coupons",
            "packaging",
            "internal-usage",
            "damage",
            "card",
            "bank",
            "reserved1",
            "reserved2"
        ]
    }
}
```

## `GET` Get Printer Info
This request gets the information about the auto-detected or config-setup properties of one specific printer. 
In this example **dy448967** is the printerId of a specific printer. 
"taxIdentificationNumber" is the Fiscal Subject Identification Number of the registered owner of the fiscal device.
If "taxIdentificationNumber" is empty string, means that it is not set.

### Example request:
```
http://localhost:8001/printers/dy448967
```

### Example response:
```json
{
    "uri": "bg.dt.c.isl.com://COM6",
    "serialNumber": "DT517985",
    "fiscalMemorySerialNumber": "02517985",
    "manufacturer": "Datecs",
    "model": "DP-25",
    "firmwareVersion": "263453 08Nov18 1312",
    "itemTextMaxLength": 22,
    "commentTextMaxLength": 40,
    "operatorPasswordMaxLength": 8,
    "taxIdentificationNumber": "000713391",
    "supportedPaymentTypes": [
        "cash",
        "coupons",
        "ext-coupons",
        "card",
        "reserved1"
    ]
}
```

## `GET` Get Printer Status
Contacts the specific fiscal printer and returns its current status and current printer date and time.
In this example **dt525860** is the printerId of a specific printer.

### Example request:
```
http://localhost:8001/printers/dt525860/status
```

### Example response:
```json
{
  "deviceDateTime": "2019-05-31T18:11:55",
  "ok": true,
  "messages": [
    {
      "type": "info",
      "text": "Serial number and number of FM are set"
    },
    {
      "type": "info",
      "text": "FM is formatted"
    }
  ]
}
```

## `POST` Print Fiscal Receipt
Prints a fiscal receipt to the specific printer. The receipt is printed as a whole transaction. 
If there is any error while printing, the printing is aborted and the receipt is voided.
In this example **dt279013** is the printerId of a specific printer.

### Example request uri:
```
http://localhost:8001/printers/dt279013/receipt
```

### JSON format of the input
Root elements
* **"uniqueSaleNumber"** - the government required unique sales number.
* **"items"** - the line items, such as fiscal line items and comments.
* **"payments"** - list of payments.

### "items"
Contains the items sold. 
The line items are printed in the same order on the fiscal printer. 
Each item record can be of different type. The type is specified in the "type" field and can be one of:
* **"sale"** - specifies items sold (default, can be omitted)
* **"discount-amount"** - specifies discount amount on the current sub-total 
* **"surcharge-amount"** - specifiessurcharge amount on the current sub-total 
* **"comment"** - comment line (printer with #)
* **"footer-comment"** - comment, printed after the payment area of the receipt

The item with type "sale" can have the following fields set:
* **"text"** - the name of the product
* **"quantiy"** - *(optional)* the quantity sold. When not specified, some fiscal printers might be able to omit the quantity completely, while others might print "1". For calculation purposes, omitting quantity means 1.
* **"unitPrice"** - the unit price, not including any discounts/markups
* **"taxGroup"** - the government regulated tax group. An integer from 1 to 8.
* **"department"** - *(optional)* the department number. A positive integer. 
* **"priceModifierValue"** - *(optional)* modifies the total amount of the line according to the setting of "priceModifierType"
* **"priceModifierType"** - *(optional)* can be one of: 
* * **"discount-percent"**
* * **"discount-amount"**
* * **"surcharge-percent"**
* * **"surcharge-amount"**

The item with type "discount-amount" and "surcharge-amount" can have the following fields set:
* **"amount"** - the amount that will be substracted or added to the subtotal
**Warning**: Check the value of "supportsSubTotalAmountModifiers" in your device info, to check whether your device supports subtotal modifiers by amount.

The item with type "comment" and "footer-comment" can have the following fields set:
* **"text"** - the text of the comment

### "payments"
This section contains the payment types and amounts for each payment. If this section is skipped, the full amount of the receipt is allocated to this payment.
NOTE: If the whole section "payments" is not provided, then the whole amount of the receipt is printed as cash payment.

NOTE: Multiple different payment types and amounts are allowed for one receipt.

Each element in this section can have the following properties:
* **"amount"** - the amount paid. 

* **"paymentType"** - one of: 
* * **"cash"** - this is the default payment type if no payment type is specified, NRA mapping "SCash"
* * **"check"** - payment with check, NRA mapping "SChecks"
* * **"card"** - payment by debit or credit card, NRA mapping "SCards"
* * **"coupons"** - payment with coupons, NRA mapping "ST"
* * **"ext-coupons"** - payment with external for the organization coupons, NRA mapping "SOT"
* * **"packaging"** - payment with returning the packaging, NRA mapping "SP"
* * **"internal-usage"** - payment for the internal usage of goods or services, NRA mapping "SSelf"
* * **"damage"** - payments about the damage, NRA mapping "SDmg"
* * **"bank"** - payment with bank transfer, NRA mapping "SW"
* * **"reserved1"** - often used by government regulations for specific purposes (health reimbursment, etc.), NRA mapping "SR1"
* * **"reserved2"** - another reserved payment, NRA mapping "SR2"


### Example request body:
```json
{
  "uniqueSaleNumber": "DT279013-0001-0000001",
  "items": [
    {
      "text": "Cheese",
      "quantity": 1,
      "unitPrice": 12,
      "taxGroup": 2
    },
    {
      "type": "comment",
      "text": "Additional comment to the cheese..."
    },
    {
      "text": "Milk",
      "quantity": 2,
      "unitPrice": 10,
      "taxGroup": 2,
      "priceModifierValue": 10,
      "priceModifierType": "discount-percent"
    },
    {
        "type": "discount-amount",
        "amount": 10
    },
    {
      "type": "footer-comment",
      "text": "YOU ARE WELCOME!"
    }
  ],
  "payments": [
    {
      "amount": 20,
      "paymentType": "cash"
    }
  ]
}
```

### Example response 1 - (No Problems) after printing receipt:
```json
{
  "ok": "true",
  "messages": [
    {
      "type": "info",
      "text": "Serial number and number of FM are set"
    },
    {
      "type": "info",
      "text": "FM is formatted"
    }
  ],
  "receiptNumber": "0000085",
  "receiptDateTime": "2019-05-17T13:55:18",
  "receiptAmount": 20,
  "fiscalMemorySerialNumber": "02517985"
}
```

### Example response 2 - (Warning) while getting the status:
```json
{
  "ok": "true",
  "messages": [
    {
      "type": "warning",
      "code": "W201",
      "text": "The fiscal memory almost full"
    },
    {
      "type": "info",
      "text": "Serial number and number of FM are set"
    },
    {
      "type": "info",
      "text": "FM is formatted"
    }
  ],
  "deviceDateTime": "2019-05-10T15:50:00"
}
```

### Example response 3 - (Error):
```json
{
  "ok": "false",
  "messages": [
    {
      "type": "error",
      "code": "E201",
      "text": "The fiscal memory is full"
    },
    {
      "type": "info",
      "text": "Serial number and number of FM are set"
    },
    {
      "type": "info",
      "text": "FM is formatted"
    }
  ]
}
```

### Return values
* **"ok"** - The **"ok"** field contains **"true"** or **"false"** to allow easy differentiation between successful or unsuccessful printing operation.
* **"receiptNumber"** - the printer internal consecutive number of the fiscal receipt,
* **"receiptDateTime"** - the date and time when the receipt was printed, according to the printer internal date and time, 
* **"receiptAmount"** - the fiscal amount, as recorded by the fiscal printer, 
* **"fiscalMemorySerialNumber"** - the serial number of the fiscal memory module, recorded in the moment of the printing.
* **"messages"** - List of fiscal device or library response messages with **"type"**, **"text"** and **"code"**.

## `POST` Print Fiscal Receipt With Operator Credentials
Prints a fiscal receipt to the specific printer with operator credentials. The syntax is the same, but two additional fields are presented, **"operator"**, and **"operatorPassword"**.

### Example request uri:
```
http://localhost:8001/printers/dt279013/receipt
```

### Example request body:
```json
{
  "uniqueSaleNumber": "DT279013-0001-0000001",
  "operator": "1",
  "operatorPassword": "1",
  "items": [
    {
      "text": "Cheese",
      "quantity": 1,
      "unitPrice": 12,
      "taxGroup": 2
    },
    {
      "text": "Additional comment to the cheese...",
      "type": "comment"
    },
    {
      "text": "Milk",
      "quantity": 2,
      "unitPrice": 10,
      "taxGroup": 2,
      "priceModifierValue": 10,
      "priceModifierType": "discount-percent"
    }
  ],
  "payments": [
    {
      "amount": 30,
      "paymentType": "cash"
    }
  ]
}
```

### Example response:  
It will be the same, as when we call Print Receipt without operator credentials.

## `POST` Print Fiscal Receipt (Async)
Asynchronously prints a fiscal receipt to the printer. The receipt is printed as a whole transaction. If there is any error while printing, the printing is aborted and the receipt is voided.

### Example request uri:
```
http://localhost:8001/printers/dt525860/receipt?asyncTimeout=0
```

### Async Execution
Asynchronous execution of print tasks allows the server to continue processing a print task after it has returned result to the caller. 
When a print task is executed asynchronously, the task is placed in a queue and the server only returns **"taskId"** to the caller.

To activate asynchronous execution, the caller should specify **"asyncTimeout"** parameter.

The **"asyncTimeout"** parameter specifies the timeout, after which the task is converted to asynchronous. Possible values are:
* **not specified** - when the parameter is not specified, the task is executed synchronously. The server waits for a printing to complete and only then returns result.
* **asyncTimeout=xxx** - the task is executed synchronously. If the task finishes before the specified timeout (xxx is number of milliseconds), task result is returned to the caller. If the task takes longer to execute, the server returns **"taskId"** result.
* **asyncTimeout=0** - the task is scheduled for execution and the server immediately returns **"taskId"** result.
NOTE: Asynchrounous execution (through the asyncTimeout parameter) is available for all print requests.

### Example request body
Тhe same as when calling Print Receipt.

### Example response 
```json
{
  "taskId": "QHC_H_7u8EaAjTB7WPEP3g"
}
```
**"taskId"** result
The **"taskId"** result is demonstated in the example return value. It contains a single **taskId** token, which can be used to later make **"taskinfo"** requests to check its status. Bellow you will see how we will use the returned **taskId** identifier **QHC_H_7u8EaAjTB7WPEP3g** to get the status of, or result information of the printed receipt.

## `POST` Print Fiscal Receipt (Idempotent)
Prints a fiscal receipt to the printer and guarantees idempotency.
Idempotency, if properly implemented, guarantees that the caller will have total guarantee whether the document was printed or not, even in the case of (temporary) network problems.

### Idempotency
In order to achieve idempotency, the caller should create (and provide it as а paramater) a unique taskId for each print task. 
This would allow the caller to properly check the task status, even in the case when the inital call has not returned response properly (due to network issues, for example).

It is suggested, that the taskId is a short, but unique string, serialized as base64 string.

### Example request uri:
```
http://localhost:8001/printers/dt525860/receipt?asyncTimeout=0&taskId=QHC_H_7u8EaAjTB7WPEP3g
```

### Example request body
Тhe same as when calling Print Receipt.

### Example response 
```json
{
  "taskId": "QHC_H_7u8EaAjTB7WPEP3g"
}
```

## `GET` Get Async Task Information
Returns information about an async printing task. Async printing tasks are created, when **"asyncTimeout"** parameter is used on any printing task. For more information about creating async printing tasks, see the documentation of "Print Fiscal Receipt (Async)".

### Example request uri:
```
http://localhost:8001/printers/taskinfo?id=QHC_H_7u8EaAjTB7WPEP3g
```

### The response:
The **"taskinfo"** action will return information about a task. The information includes:

**taskStatus** - This is one of: 
* **"unknown"** - there is no information about the task (see below),  
* **"enqueued"** - the task is waiting in the queue, 
* **"running"** - the task is being currently executed, 
* **"finished"** - the task has finished, 
* **"result"** - only returned for "finished" tasks. Contains the data, returned by the task.
When a print task is created, it is put in a FIFO print queue. If the user requests a synchronous print method, the method returns result only after all previously enqueued tasks and the current task have finished.

Therefore, it is always recommended to use async requests.

Upon finishing a print task, the server moves it to a "completed" list of print tasks. The information about the completed task sits in the "completed" list up to the restart of the service. After this, the information about the print task is not available and further requests for information about this task will return unknown status.

When the caller requests info about a task and the returned status is finished, the server also deletes the information about the task (and further inquiries about this task status will return unknown).

## `POST` Print Reversal Receipt
Prints a receipt, which reverses another (normal) receipt.

NOTE: The quantities and amounts in the reversal receipt are again POSITIVE values. You do not have to specify minus values.

### Example request uri:
```
http://localhost:8001/printers/dt525860/reversalreceipt
```

The reversal receipt JSON input format is mostly the same as PrintReceipt. The call also requires some additional values, which were obtained as result information from the PrintReceipt call. The fields in addition/changed, compared to PrintReceipt are:

* **"uniqueSaleNumber"** - should contain the same number, which was on the original receipt. You MUST NOT create new unique number for the reversal.
* **"receiptNumber"** (obtained from the original receipt return info)
* **"receiptDateTime"** (obtained from the original receipt return info)
* **"fiscalMemorySerialNumber"** (obtained from the original receipt return info)
* **"reason"** - the reason for the reversal. One of: 
* * **"operator-error"**
* * **"refund"**
* * **"taxbase-reduction"**. (it should be "tax-base-reduction", but because of the backward compatibility issues, we will stick with "taxbase-reduction").

### Response
The same as PrintReceipt, except for the "info" section, which is not provided (not needed).

## `POST` Print Deposit Money Receipt
Deposits the amount

Warning: Be aware, in Tremol devices, you should present credentials - operator and operatorPassword as in receipt request.

### Example request uri:
```
http://localhost:8001/printers/dt517985/deposit
```
### Example request body:
```json
{
  "amount": 12.34
}
```

### Response
The response is standard status response.

## `POST` Print Withdraw Money Receipt
Withdraws the amount

Warning: Be aware, in Tremol devices, you should present credentials - operator and operatorPassword as in receipt request.

### Example request uri:
```
http://localhost:8001/printers/dt517985/withdraw
```
### Example request body:
```json
{
  "amount": 12.34
}
```

### Response
The response is standard status response.

## `POST` Print X Report
Prints the Turnover Report

### Example request uri:
```
http://localhost:8001/printers/dt517985/xreport
```

### Response
The response is standard status response.

## `POST` Print Z Report
Prints and Zeroes the Turnover Report

### Example request uri:
```
http://localhost:8001/printers/dt517985/zreport
```

### Response
The response is standard status response.


## `POST` Set Printer Date And Time
Sets the date and time of the fiscal printer. You should use the format for **deviceDateTime** emitted by javascript Date's object, toJSON method, it conforms to ISO 8601.

### Example request uri:
```
http://localhost:8001/printers/zk126720/datetime
```

### Example request body:
```json
{
  "deviceDateTime": "2019-05-31T18:06:00"
}
```

### Response
The response is standard status response.


## `GET` Get Current Cash Amount
Gets the current cash amount registered in the fiscal printer. 
The request is `POST` because you are allowed to provide "taskId" in the body of the request.

Warning: Be aware, in Tremol devices, you should present credentials - operator and operatorPassword as in receipt request.

### Example request uri:
```
http://localhost:8001/printers/dt525860/cash
```

### Example response 
```json
{
    "amount": 12.34,
    "ok": true,
    "messages": [
        {
            "type": "info",
            "text": "Serial number and number of FM are set"
        },
        {
            "type": "info",
            "text": "FM is formatted"
        }
    ]
}
```

## `POST` Post Raw Request
Post raw request to the fiscal device and expects device status and raw response.

### Example for FP-2000, singing Ode of Joy

The first character "P" is the code for the command. Next charaters are the request data.

```json
{
    "rawRequest": "PB02B02C02D02D02C02B02A02G02G02A02B02B03A01A02 02B02B02C02D02D02C02B02A02G02G02A02B02A03G01G02"
}
```

### Example for FP-2000
Command
56H (86) Obtain the Date of the last record in fiscal memory

0x56 is the ASCII code for "V". 

"T" is the parameter for command, which gives not only Date, but also the Time of the last record in FM.

```json
{
	"rawRequest": "VT"
}
```

### Example response for FP-2000

The answer is here: "rawResponse": "25-06-2019 11:15:26".

```json
{
    "rawResponse": "25-06-2019 11:15:26",
    "ok": true,
    "messages": [
        {
            "type": "info",
            "text": "No customer display is connected"
        },
        {
            "type": "warning",
            "code": "W301",
            "text": "Low paper"
        },
        {
            "type": "info",
            "text": "SW7=ON, SW6=ON, SW5=OFF, SW4=ON, SW3=OFF, SW2=ON, SW1=OFF"
        },
        {
            "type": "info",
            "text": "Unique Printer ID and Fiscal Memory ID are set"
        },
        {
            "type": "info",
            "text": "BULSTAT UIC is set"
        },
        {
            "type": "info",
            "text": "The tax rates are set at least once"
        },
        {
            "type": "info",
            "text": "The printer is in fiscal mode"
        },
        {
            "type": "info",
            "text": "The fiscal memory is formatted"
        }
    ]
}
```

## `POST` Print Last Receipt's Duplicate
Prints duplicate of the last fiscal receipt

### Example request uri:
```
http://localhost:8001/printers/dt517985/duplicate
```

### Response
The response is standard status response.

