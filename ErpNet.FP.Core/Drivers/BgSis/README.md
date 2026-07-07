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
