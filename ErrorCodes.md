# Standardized Error And Warning Codes
This file contains the standard error codes of the ErpNet.FP library.

The standard error and warning code is returned in the "code" field of the "messages" array in the result set of each POST operation.

The standard error codes are guaranteed to be the same for all printer manufacturers. 
The library checks the original manufacturer error code, bit flags and statuses and selects a standardized message code.
The standard error and warning codes allow the consumer of the library to work with the same code, 
despite the underlying printer manufacturer or printer revision.

If the consumer wants access to the more detailed manufacturer error code, the "originalCode" field can be used.
However, the "originalCode" might not always be available or it might change due to different reasons.

# Error Codes

## Device error codes 
* **E101** Device not responding
* **E102** Low battery
* **E103** Date and time not set or invalid
* **E104** RAM has been reset
* **E104** RAM failure
* **E106** External display required
* **E107** Invalid device response
* **E199** Device general error

## Fiscal memory and journal error codes
* **E201** Fiscal Memory is full
* **E202** Fiscal Memory store error
* **E203** Fiscal Memory read error
* **E204** Fiscal Memory is locked in read-only mode
* **E205** No Fiscal Memory module
* **E206** Electronic Journal/SD Card is full
* **E207** Wrong Electronic Journal/SD Card 
* **E299** Fiscal Memory general error

## Printing error codes
* **E301** Out of paper
* **E302** Printer cover open
* **E303** Printing unit fault
* **E304** Printing head overheated
* **E305** Power down while printing
* **E306** Error in paper cutter
* **E399** Printing general error

## Command error codes
* **E401** Syntax error
* **E402** Invalid command
* **E403** Value overflow or underflow (value out of bounds)
* **E404** Illegal command, not allowed in this mode
* **E405** Insufficient conditions
* **E406** Invalid payment
* **E407** Invalid item
* **E408** Wrong password or access denied
* **E409** Wrong command response format
* **E410** Document is empty or no items
* **E411** Invalid tax group
* **E499** Command general error

## NRA link error codes
* **E501** NRA report fail
* **E502** 24hours block – missing Z report
* **E503** Blocking 3 days without mobile operator
* **E504** Wrong SIM card
* **E505** No GPRS service
* **E506** No mobile operator
* **E507** No GPRS Modem
* **E508** No SIM card
* **E599** NRA link general error

## External or referenced errors
* **E999** Error from another reference, i.e. find and read the error code description in the device/operations/users manual 


# Warning Codes

## Fiscal memory warning codes
* **W201** Fiscal Memory near full
* **W202** Electronic Journal/SD Card near full
* **W299** General fiscal memory warning

## Printing warning codes
* **W301** Low paper/Near end of paper
* **W399** Printing general warning

## NRA link warning codes
* **W501** Unsent data for 24 hours
* **W502** Report not zeroed
* **W599** NRA link general warning
