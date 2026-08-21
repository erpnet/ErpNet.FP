# SIS Fiscal Module driver (`bg.sis.json`)

This driver integrates the SIS Technology fiscal modules (JSON-RPC 2.0 over HTTP)
into ErpNet.FP.

## About the protocol specification

Unlike the other drivers in this repository, **no protocol specification document is
included here on purpose.**

SIS Technology granted permission to publish this integration (the driver source code),
but **not** to redistribute the protocol specification itself. Please **do not add the SIS
specification (PDF or extracted text) to this folder or anywhere in the repository.**

For the full protocol specification, contact SIS Technology directly:
https://sistechnology.com/

## Supported models

MF-P1200DN, MF-TH230QR, MF-TH250QR, BULPRINT T2QR, BULPRINT T3QR.

Network devices are configured explicitly (they are not auto-detected). See `Config.md`
and `README.md` in the repository root for the Uri format and per-device options.

## Device-specific limits

### Digits in comment lines

An item of type `"comment"` accepts digits **only in its first 31 characters**. A digit past
that position is rejected by the fiscal module with `EM_PARA_WRONG_FORMAT` (`0x5D`) - an error
that says nothing about which line or which character caused it.

The driver therefore validates comments up front and fails the whole document before anything
is sent to the device:

```
E403  Item {N}: a comment line accepts digits only in the first 31 characters
```

* `{N}` is the 1-based position of the item in the `"items"` array, counting **every** item -
  not the position among the comments only.
* Nothing reaches the device, so no fiscal document is opened and no paper is spent.
* Characters are counted, not bytes - a Cyrillic letter counts as one.
* The check applies to receipts, reversal receipts, invoices and credit notes alike, as all
  four validate through `ValidateReceipt`.

**Not affected** - digits may appear anywhere in:

* the `"text"` of a `"sale"` item,
* `"footer-comment"` items,
* the text of subtotal `"discount-amount"` / `"surcharge-amount"` items.

That exemption was verified on hardware. Do not assume the rule extends to other text fields.

The usual way to hit this is a long label that pushes the number too far to the right:

```
Клиентски номер по договор: 123456
                            ^^^^ the 4th digit sits at position 32 -> E403
```

Shorten the label, or put the number on a comment line of its own.
