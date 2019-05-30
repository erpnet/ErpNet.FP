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
* **ERR01** - Device not responding
* **ERR02** - Printer is out of paper
* **ERR03** - Fiscal memory is full
* **ERR04** - Revenue agency reporting failed

# Warning Codes
* **WRN01** - Device cutter is not functioning correctly
* **WRN02** - Revenue agency reporting temporary problem
