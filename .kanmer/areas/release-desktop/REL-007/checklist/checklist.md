# Checklist — REL-007

Derived from `plan`, one box per step, in plan order. Boxes marked **(operator)** are
performed by the operator; an agent prepares the command and the evidence template and
records the result. Tick with `set_ticket_doc(doc: "checklist")`; append progress notes
below rather than rewriting.

- [ ] Read the area plan § 5 row `DSK-09-08`, `signing-and-hosting-decision-matrix.md` § D-002 in full, and `runbooks.md` § R5 and § R7; run `get_doc_gates REL-007` and `take_ticket REL-007`
- [ ] Read `Identity/@Publisher` from `src/Pegasus.Desktop/Package.appxmanifest` and paste it verbatim into the `plan` document in a fenced block as the canonical subject string
- [ ] Cross-check that canonical string against the subject `DSK-09-06` (board `REL-006`) recorded for the development certificate, and confirm they are identical
- [ ] Run `microsoft_docs_search` then `microsoft_docs_fetch` for the `New-SelfSignedCertificate` code-signing page, and record its URL, fetch date and the confirmed flag set (including both `-TextExtension` OIDs) in the `plan` document
- [ ] **(operator)** Issue the certificate on the signing host with the verified command plus `-NotAfter (Get-Date).AddYears(3)`, and hand back thumbprint, subject, `NotBefore`, `NotAfter` and the machine name
- [ ] **(operator)** Export the `.pfx` with `Export-PfxCertificate` to a secure path **outside any git working tree**, and hand back the path
- [ ] **(operator)** Restrict the `.pfx` ACL with `icacls <path> /inheritance:r /grant "<publisher account>:(R)"` and hand back the `icacls` output
- [ ] **(operator)** Export the public certificate only with `Export-Certificate … -Type CERT` and hand back the `.cer`
- [ ] Create `eng/packaging/Install-ProductionCertificateTrust.ps1` with the repository script header and mandatory `-CertificatePath` and `-ExpectedThumbprint` parameters, starting from `eng/packaging/Install-DevCertificateTrust.ps1` so the two do not diverge
- [ ] Add the elevation assertion that throws a sentence naming the requirement when the session is not elevated
- [ ] Add `Import-Certificate -FilePath <cer> -CertStoreLocation Cert:\LocalMachine\TrustedPeople` with the store rationale as a comment **at the import line**, and the `certutil -verifystore TrustedPeople` thumbprint assertion that fails when `-ExpectedThumbprint` is absent
- [ ] **(operator)** Machine A: run the trust script, confirm the thumbprint is listed, sign a test package with `--timestamp`, install it, and hand back the transcript
- [ ] **(operator)** Machine B: attempt `Add-AppxPackage` **before** any trust step, record the exact failure (expected `0x800B0109`), then run the trust script and retry successfully; hand back both transcripts
- [ ] Run `signtool verify /pa /v <pkg>.msix` and confirm the output contains **both** a valid chain and a timestamp line — not merely exit `0`
- [ ] **(operator)** Establish whether the estate is domain-joined (`(Get-CimInstance Win32_ComputerSystem).PartOfDomain` or the operator's answer) and record the command or answer
- [ ] **(operator)** Choose the estate mechanism — scripted elevated `Import-Certificate` or GPO Trusted People — and record which applies, why, and the machine list in the `plan` document
- [ ] **(operator)** Roll trust to the remaining workstations and hand back a per-machine confirmation line (machine name, date, `certutil -verifystore TrustedPeople` thumbprint)
- [ ] Confirm trust has reached **every** machine before `DSK-09-11` (board `REL-009`) publishes any signed package (R1 precondition 4)
- [ ] Write the renewal and revocation rehearsal write-up (same subject on re-issue, overlap window, trust before first signature, old `.cer` removed last, no revocation list in a private-trust estate) and hand it to `DSK-09-14` (board `REL-012`)
- [ ] Name the signing-host concentration risk in the write-up and point its mitigations at R9 (`DSK-09-10`, board `REL-008`) and the security plan rather than solving them here
- [ ] Record the certificate facts for the release record — thumbprint, subject, validity window, signing host, rollout date, confirmed machines — for `DSK-09-18` (board `REL-016`)
- [ ] Record the chosen estate mechanism and the confirmed machine list in `docs/desktop/09-release-update-and-distribution/runbooks.md` § R5 and § R7
- [ ] Verification run: `certutil -verifystore TrustedPeople` on each workstation lists the thumbprint; `signtool verify /pa /v` reports chain and timestamp; `Add-AppxPackage` succeeds on a trusted machine and fails with `0x800B0109` on an untrusted one; `icacls <pfx path>` shows only the publisher account with read and inheritance removed — this box produces `proof`
- [ ] Confirm in the proof that `NotAfter` is approximately three years out and that the certificate subject is byte-identical to `Identity/@Publisher`
- [ ] Record the dated `## Simplification pass` in the `plan` document over this branch's own diff (not `n/a — docs-only`; this branch adds a script and edits a runbook)

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
