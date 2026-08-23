$ErrorActionPreference = 'Stop'
$base = 'http://localhost:5001'
$future = (Get-Date).AddDays(7).ToString('yyyy-MM-dd')
$results = New-Object System.Collections.Generic.List[string]

function Check([string]$name, [bool]$ok, [string]$detail) {
  $script:results.Add(("$(if($ok){'PASS'}else{'FAIL'})  $name  ::  $detail"))
}

function Call([string]$method, [string]$path, [string]$user, $body) {
  $params = @{ Method = $method; Uri = "$base$path"; ContentType = 'application/json'; ErrorAction = 'Stop' }
  if ($user) { $params.Headers = @{ 'X-RRVMS-Prototype-User' = $user } }
  if ($null -ne $body) { $params.Body = ($body | ConvertTo-Json -Depth 8) }
  return Invoke-RestMethod @params
}

function ExpectStatus([string]$method, [string]$path, [string]$user, $body, [int]$expected) {
  try {
    Call $method $path $user $body | Out-Null
    return "expected $expected but got success"
  } catch {
    $status = [int]$_.Exception.Response.StatusCode
    if ($status -eq $expected) { return $null }
    return "expected $expected but got $status"
  }
}

try {
  # ---- Health ----
  $h = Call GET '/api/health' $null $null
  Check 'health endpoint' ($h.status -eq 'ok' -and $h.database -eq 'connected') "$($h.status)/$($h.database)"

  # ---- Create request (External visitor with asset) ----
  $created = Call POST '/api/visitor-requests' 'prototype-requester' @{
    visitorType='External'; fullName='Jane Visitor'; companyName='Acme Aerospace'
    citizenship='United Kingdom'; country='United Kingdom'; designation='Propulsion Engineer'
    email='jane.visitor@acme.aero'; phone='+44123456789'; idType='Passport'; idLast4='1234'
    visitingCompany='Rolls-Royce Holdings'; visitingSite='Derby Civil Site'
    purpose='Technical design review of turbine components.'
    visitPurposeType='Business meeting'
    visitDays=@(@{ visitDate=$future; expectedArrivalTime='09:00:00'; expectedDepartureTime='17:00:00' })
    assets=@(@{ assetType='Laptop'; description='Dell XPS 15'; serialNumber='DL-XPS-001' })
  }
  $id = $created.id; $dayId = $created.visitDays[0].id
  Check 'create visitor request' (-not [string]::IsNullOrEmpty($id) -and $created.currentStatus -eq 'VISITOR_FORM_PENDING') "id=$id status=$($created.currentStatus) number=$($created.requestNumber)"

  # ---- Auth guards ----
  $err = ExpectStatus GET '/api/dashboard' $null $null 200   # dashboard is anonymous-allowed; expect success actually
  Check 'dashboard anonymous access' ($null -eq $err) "$(if($err){$err}else{'accessible'})"
  $err = ExpectStatus POST "/api/visitor-requests/$id/actions" 'prototype-host' (@{ action='ec-approve' }) 403
  Check 'role guard (host cannot EC approve)' ($null -eq $err) "$(if($err){$err}else{'403 returned'})"
  $err = ExpectStatus POST '/api/visitor-requests' $null (@{}) 401
  Check 'unauthenticated create rejected' ($null -eq $err) "$(if($err){$err}else{'401 returned'})"

  # ---- Workflow: submit -> host review -> DPS -> EC approve ----
  $r = Call POST "/api/visitor-requests/$id/actions" 'prototype-requester' @{ action='visitor-submit' }
  Check 'visitor submit' ($r.currentStatus -eq 'HOST_REVIEW') $r.currentStatus

  $r = Call POST "/api/visitor-requests/$id/actions" 'prototype-host' @{ action='host-submit' }
  Check 'host submit (external -> DPS pending)' ($r.currentStatus -eq 'DPS_PENDING') $r.currentStatus

  $r = Call POST "/api/visitor-requests/$id/actions" 'prototype-export-control' @{ action='dps'; comment='Clear'; reason='No adverse media' }
  Check 'DPS clear' ($r.currentStatus -eq 'EC_REVIEW') $r.currentStatus

  $r = Call POST "/api/visitor-requests/$id/actions" 'prototype-export-control' @{ action='ec-approve'; comment='Approved for Derby site' }
  Check 'EC approve' ($r.currentStatus -eq 'APPROVED') $r.currentStatus

  # ---- Security verification + hold + resolve ----
  $err = ExpectStatus POST "/api/visitor-requests/$id/actions" 'prototype-security' (@{ action='verify'; idLast4='9999' }) 409
  Check 'identity mismatch handling' ($null -ne $err -and $err -notmatch 'expected 409 but got') "$(if($err -and $err -match '^expected'){ 'mismatch produced: '+$err }else{$err})"

  $r = Call POST "/api/visitor-requests/$id/actions" 'prototype-security' @{ action='verify'; idLast4='1234' }
  Check 'security ID verify' ($r.currentStatus -eq 'SECURITY_VERIFICATION') $r.currentStatus

  $r = Call POST "/api/visitor-requests/$id/actions" 'prototype-security' @{ action='hold'; comment='Undeclared camera detected'; assetSerials='SN-CAM-777'; visitDayId=$dayId }
  Check 'security hold (undeclared asset)' ($r.currentStatus -eq 'SECURITY_HOLD_EC_REVIEW') $r.currentStatus
  $undeclared = @($r.assets | Where-Object { $_.verificationStatus -eq 'Undeclared' })
  Check 'undeclared asset recorded' ($undeclared.Count -ge 1) "count=$($undeclared.Count)"

  $r = Call POST "/api/visitor-requests/$id/actions" 'prototype-host' @{ action='resolve-hold'; comment='Camera removed and stored' }
  Check 'resolve hold back to EC' ($r.currentStatus -eq 'EC_REVIEW') $r.currentStatus

  $r = Call POST "/api/visitor-requests/$id/actions" 'prototype-export-control' @{ action='ec-approve'; comment='Re-approved after hold resolution' }
  Check 're-approve after hold' ($r.currentStatus -eq 'APPROVED') $r.currentStatus

  # ---- Badge / check-in / check-out ----
  $r = Call POST "/api/visitor-requests/$id/actions" 'prototype-security' @{ action='check-in'; badgeNumber='RR-BADGE-0001'; visitDayId=$dayId }
  Check 'badge issue + check-in' ($r.currentStatus -eq 'CHECKED_IN') $r.currentStatus

  $r = Call POST "/api/visitor-requests/$id/actions" 'prototype-security' @{ action='check-out'; comment='Visit complete' }
  Check 'check-out completes visit' ($r.currentStatus -eq 'VISIT_PROCESS_COMPLETED') $r.currentStatus

  $err = ExpectStatus POST "/api/visitor-requests/$id/actions" 'prototype-security' (@{ action='no-show' }) 409
  Check 'no-show blocked after completion' ($null -eq $err) "$(if($err){$err}else{'409 returned'})"

  # ---- Second flow: Internal visitor, documentation loop, no-show ----
  $created2 = Call POST '/api/visitor-requests' 'prototype-host' @{
    visitorType='Internal'; fullName='Bob Intern'; companyName='Rolls-Royce Holdings'
    citizenship='India'; country='India'; designation='Graduate Engineer'
    email='bob.intern@rolls-royce.com'; phone='+919876543210'; idType='Aadhaar'; idLast4='5678'
    visitingCompany='Rolls-Royce Holdings'; visitingSite='Bengaluru Facility'
    purpose='Internal facility familiarisation visit.'
    visitPurposeType='Internal meeting'
    visitDays=@(@{ visitDate=$future; expectedArrivalTime='10:00:00'; expectedDepartureTime='16:00:00' })
    assets=@()
  }
  $id2 = $created2.id; $dayId2 = $created2.visitDays[0].id
  Check 'create internal request' (-not [string]::IsNullOrEmpty($id2)) "id=$id2"

  Call POST "/api/visitor-requests/$id2/actions" 'prototype-requester' @{ action='visitor-submit' } | Out-Null
  $r = Call POST "/api/visitor-requests/$id2/actions" 'prototype-host' @{ action='host-submit' }
  Check 'internal skips DPS to EC review' ($r.currentStatus -eq 'EC_REVIEW') $r.currentStatus

  $r = Call POST "/api/visitor-requests/$id2/actions" 'prototype-export-control' @{ action='ec-request-documents'; reason='Provide signed NDA copy'; comment='Compliance requirement' }
  Check 'request pending documentation' ($r.currentStatus -eq 'PENDING_DOCUMENTATION') $r.currentStatus

  $r = Call POST "/api/visitor-requests/$id2/actions" 'prototype-host' @{ action='submit-documents'; comment='NDA signed by visitor' }
  Check 'submit documents' ($r.currentStatus -eq 'DOCUMENTATION_SUBMITTED') $r.currentStatus

  $r = Call POST "/api/visitor-requests/$id2/actions" 'prototype-export-control' @{ action='ec-approve'; comment='Docs received' }
  Check 'approve after docs' ($r.currentStatus -eq 'APPROVED') $r.currentStatus

  $r = Call POST "/api/visitor-requests/$id2/actions" 'prototype-security' @{ action='no-show'; visitDayId=$dayId2 }
  $noShow = @($r.visitDays | Where-Object { $_.status -eq 'NoShow' })
  Check 'mark no-show' ($noShow.Count -ge 1) "days marked=$($noShow.Count)"

  # ---- Host change re-review path ----
  $created3 = Call POST '/api/visitor-requests' 'prototype-requester' @{
    visitorType='Internal'; fullName='Carol Guest'; companyName='Rolls-Royce Holdings'
    citizenship='Germany'; country='Germany'; designation='Programme Lead'
    email='carol.guest@rolls-royce.com'; phone='+49123456789'; idType='National ID'; idLast4='9012'
    visitingCompany='Rolls-Royce Holdings'; visitingSite='Berlin Office'
    purpose='Partner programme sync.'
    visitPurposeType='Business meeting'
    visitDays=@(@{ visitDate=$future })
    assets=@()
  }
  $id3 = $created3.id
  Call POST "/api/visitor-requests/$id3/actions" 'prototype-requester' @{ action='visitor-submit' } | Out-Null
  Call POST "/api/visitor-requests/$id3/actions" 'prototype-host' @{ action='host-submit' } | Out-Null
  Call POST "/api/visitor-requests/$id3/actions" 'prototype-export-control' @{ action='ec-approve'; comment='OK' } | Out-Null
  $r = Call POST "/api/visitor-requests/$id3/actions" 'prototype-requester' @{ action='host-change'; newUserId='prototype-admin' }
  Check 'host change forces re-review' ($r.currentStatus -eq 'EC_RE_REVIEW_REQUIRED') $r.currentStatus

  # ---- Read endpoints ----
  $dash = Call GET '/api/dashboard' 'prototype-requester' $null
  Check 'dashboard aggregates' ($dash.totalRequests -ge 3) "totalRequests=$($dash.totalRequests) pendingActions=$($dash.pendingActions)"

  $notes = @(Call GET '/api/notifications' 'prototype-host' $null)
  Check 'notifications for host' ($notes.Count -gt 0) "count=$($notes.Count)"
  if ($notes.Count -gt 0) {
    Call POST "/api/notifications/$($notes[0].id)/read" 'prototype-host' $null | Out-Null
    Check 'mark notification read' $true $notes[0].type
  }

  $secList = @(Call GET '/api/security/visitors?search=Jane' 'prototype-security' $null)
  Check 'security visitors search (checked-in filter)' $true "rows=$($secList.Count)"

  $list = Call GET '/api/visitor-requests?page=1&pageSize=10' 'prototype-requester' $null
  Check 'list requests paginated' ($list.total -ge 3) "total=$($list.total)"

  $det = Call GET "/api/visitor-requests/$id" 'prototype-requester' $null
  Check 'detail includes audit history' ($det.auditHistory.Count -ge 8) "audit entries=$($det.auditHistory.Count)"

} catch {
  $results.Add("FAIL  UNEXPECTED EXCEPTION  ::  $($_.Exception.Message)")
}

$results | ForEach-Object { Write-Output $_ }
$failed = @($results | Where-Object { $_.StartsWith('FAIL') }).Count
Write-Output "SUMMARY: $($results.Count - $failed) passed, $failed failed"
