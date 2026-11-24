/* ===== Config: change this if your API runs on a different address ===== */
const API_BASE = 'http://localhost:5229'; // must match the API's http applicationUrl (launchSettings.json); 5500 is reserved for Live Server

const $ = (s, r = document) => r.querySelector(s), app = $('#app');
const esc = s => String(s ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
const S = { token: localStorage.getItem('token'), user: JSON.parse(localStorage.getItem('user') || 'null'), cats: [], locs: [] };
const isAdmin = () => S.user?.role === 'Admin';
const go = p => { location.hash = '#' + p; };
const fd = d => new Date(d).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' });
const cn = id => S.cats.find(c => c.categoryID === id)?.name || '—';
const ln = id => S.locs.find(l => l.locationID === id)?.name || '—';
const opt = (a, sel) => a.map(([v, l]) => `<option value="${esc(v)}" ${String(v) === String(sel) ? 'selected' : ''}>${esc(l)}</option>`).join('');

function toast(m, err) {
  const t = $('#toast'); t.textContent = m; t.className = 'show' + (err ? ' err' : '');
  clearTimeout(toast.t); toast.t = setTimeout(() => t.className = '', 3500);
}

async function api(path, { method = 'GET', query, body } = {}) {
  const q = new URLSearchParams(Object.entries(query || {}).filter(([, v]) => v !== '' && v != null));
  const res = await fetch(API_BASE + path + (q.toString() ? '?' + q : ''), {
    method,
    headers: { ...(body && !(body instanceof FormData) ? { 'Content-Type': 'application/json' } : {}), ...(S.token ? { Authorization: 'Bearer ' + S.token } : {}) },
    body: body ? (body instanceof FormData ? body : JSON.stringify(body)) : undefined
  }).catch(() => { throw new Error('Cannot reach the server. Is the API running at ' + API_BASE + ' ?'); });
  if (res.status === 401 && S.token) { logout(); throw new Error('Session expired. Please log in again.'); }
  if (res.status === 403) throw new Error('You are not allowed to do that.');
  const txt = await res.text(); let data = null;
  try { data = txt ? JSON.parse(txt) : null; } catch { }
  if (!res.ok) throw new Error(data?.message || data?.title || 'Request failed (' + res.status + ')');
  return data;
}

function setAuth(token, user) {
  S.token = token; S.user = user;
  token ? (localStorage.setItem('token', token), localStorage.setItem('user', JSON.stringify(user))) : (localStorage.removeItem('token'), localStorage.removeItem('user'));
}
function logout() { setAuth(null, null); go('/login'); }

function nav() {
  const h = location.hash || '#/', on = p => h.startsWith('#/' + p) ? 'on' : '';
  $('#nav').innerHTML = `<a class="${h === '#/' ? 'on' : ''}" href="#/">Browse</a><a class="${on('new')}" href="#/new/lost">Report</a>` +
    (S.user ? `<a class="${on('me')}" href="#/me">My account</a>${isAdmin() ? `<a class="${on('admin')}" href="#/admin">Admin</a>` : ''}<button id="lo">Logout (${esc(S.user.username)})</button>`
      : `<a class="${on('login')}" href="#/login">Login</a><a class="btn sm" href="#/register">Sign up</a>`);
  $('#lo')?.addEventListener('click', logout);
}

/* ---------- components ---------- */
const photoUrl = id => `${API_BASE}/api/reports/${id}/photo`;
const img = r => r.hasPhoto ? `<img src="${photoUrl(r.reportID)}" alt="" onerror="this.remove()">` : '';

/* Shrinks big photos in the browser (max 1280px, JPEG) so uploads are fast and the DB stays small. */
async function prepImage(file) {
  if (!file.type.startsWith('image/')) throw new Error('Please choose an image file.');
  if (file.type === 'image/gif' && file.size <= 5 * 1024 * 1024) return file;
  try {
    const bmp = await createImageBitmap(file), k = Math.min(1, 1280 / Math.max(bmp.width, bmp.height));
    const c = document.createElement('canvas'); c.width = Math.round(bmp.width * k); c.height = Math.round(bmp.height * k);
    const x = c.getContext('2d'); x.fillStyle = '#fff'; x.fillRect(0, 0, c.width, c.height); x.drawImage(bmp, 0, 0, c.width, c.height);
    const blob = await new Promise(r => c.toBlob(r, 'image/jpeg', 0.85));
    if (blob) return new File([blob], 'photo.jpg', { type: 'image/jpeg' });
  } catch { }
  if (file.size > 5 * 1024 * 1024) throw new Error('Photo is too large (max 5 MB).');
  return file;
}
const card = r => `<a class="card" href="#/report/${r.reportID}"><div class="ph">${img(r)}</div><div class="cb">
  <span class="badge ${r.type}">${r.type}</span> <span class="badge ${r.status}">${r.status}</span>
  <h3>${esc(r.title)}</h3><p class="muted">📍 ${esc(ln(r.locationID))}<br>🏷 ${esc(cn(r.categoryID))} · ${fd(r.reportDate)}</p></div></a>`;
const grid = (l, msg) => l.length ? `<div class="grid">${l.map(card).join('')}</div>` : `<div class="empty">${msg}</div>`;

/* ---------- pages ---------- */
const F = { type: '', status: 'Open', categoryID: '', locationID: '', searchText: '', pageNumber: 1 }, PS = 12;
async function home() {
  app.innerHTML = `<section class="hero"><h1>Lost something? Found something?</h1><p>Report it here and help items get back to their owners.</p>
  <div class="row"><a class="btn" href="#/new/lost">I lost an item</a><a class="btn alt" href="#/new/found">I found an item</a></div></section>
  <form id="flt" class="filters"><input name="searchText" placeholder="Search by keyword…" value="${esc(F.searchText)}">
  <select name="type">${opt([['', 'All types'], ['Lost', 'Lost'], ['Found', 'Found']], F.type)}</select>
  <select name="categoryID">${opt([['', 'All categories'], ...S.cats.map(c => [c.categoryID, c.name])], F.categoryID)}</select>
  <select name="locationID">${opt([['', 'All locations'], ...S.locs.map(l => [l.locationID, l.name])], F.locationID)}</select>
  <select name="status">${opt([['', 'Any status'], ['Open', 'Open'], ['Resolved', 'Resolved'], ['Closed', 'Closed']], F.status)}</select>
  <button class="btn">Search</button></form><div id="list"></div><div id="pager" class="pager"></div>`;
  const fl = $('#flt');
  fl.onsubmit = e => { e.preventDefault(); Object.assign(F, Object.fromEntries(new FormData(fl)), { pageNumber: 1 }); load(); };
  fl.querySelectorAll('select').forEach(s => s.onchange = () => fl.requestSubmit());
  async function load() {
    const list = await api('/api/reports', { query: { ...F, pageSize: PS } });
    $('#list').innerHTML = grid(list, 'No reports match your search.');
    $('#pager').innerHTML = (F.pageNumber > 1 || list.length === PS) ? `<button class="btn sm gray" id="pv" ${F.pageNumber < 2 ? 'disabled' : ''}>← Prev</button><span>Page ${F.pageNumber}</span><button class="btn sm gray" id="nx" ${list.length < PS ? 'disabled' : ''}>Next →</button>` : '';
    if ($('#pv')) { $('#pv').onclick = () => { F.pageNumber--; load(); }; $('#nx').onclick = () => { F.pageNumber++; load(); }; }
  }
  await load();
}

async function detail(id) {
  const r = await api('/api/reports/' + id);
  const mine = S.user?.userID === r.userID, manage = mine || isAdmin(), open = r.status === 'Open';
  app.innerHTML = `<a href="#/">← Back to reports</a><div class="detail" style="margin-top:14px"><div class="ph">${img(r)}</div><div>
  <span class="badge ${r.type}">${r.type}</span> <span class="badge ${r.status}">${r.status}</span><h1>${esc(r.title)}</h1>
  <p style="white-space:pre-wrap">${esc(r.description)}</p>
  <dl><dt>Category</dt><dd>${esc(cn(r.categoryID))}</dd><dt>Location</dt><dd>${esc(ln(r.locationID))}</dd><dt>${r.type === 'Lost' ? 'Date lost' : 'Date found'}</dt><dd>${fd(r.reportDate)}</dd></dl>
  ${manage ? `<div class="row" style="justify-content:flex-start">
    <a class="btn sm alt" href="#/edit/${r.reportID}">Edit</a>
    ${open && (isAdmin() || r.type === 'Lost') ? `<button class="btn sm" data-s="Resolved">Mark resolved</button>` : ''}
    ${open ? `<button class="btn sm gray" data-s="Closed">Close</button>` : ''}
    <button class="btn sm danger" id="del">Delete</button></div>` : ''}</div></div>
  ${r.type === 'Found' && open && !mine ? `<div class="panel mid" style="margin-top:22px"><h2 style="margin-top:0">This is mine</h2>${S.user
      ? `<form id="clm"><label>Describe proof of ownership</label><textarea name="proof" rows="4" required placeholder="Colour, brand, marks, contents, serial number…"></textarea><p><button class="btn">Submit claim</button></p></form>`
      : `<p><a href="#/login">Log in</a> to submit a claim for this item.</p>`}</div>` : ''}
  <h2>Possible matches</h2><div id="mt"></div>`;
  app.querySelectorAll('[data-s]').forEach(b => b.onclick = async () => {
    await api(`/api/reports/${id}/status`, { method: 'PATCH', query: { newStatus: b.dataset.s } }); toast('Status updated'); route();
  });
  $('#del')?.addEventListener('click', async () => {
    if (!confirm('Delete this report permanently?')) return;
    await api('/api/reports/' + id, { method: 'DELETE' }); toast('Report deleted'); go('/');
  });
  $('#clm')?.addEventListener('submit', async e => {
    e.preventDefault();
    try { await api('/api/claims', { method: 'POST', body: { reportID: id, proof: e.target.proof.value } }); toast('Claim submitted. An admin will review it.'); go('/me'); }
    catch (er) { toast(er.message, 1); }
  });
  $('#mt').innerHTML = grid(await api(`/api/reports/${id}/matches`).catch(() => []), 'No matches yet.');
}

async function form(mode, arg) {
  if (!S.user) return go('/login');
  let r = { type: arg === 'found' ? 'Found' : 'Lost', title: '', description: '', reportDate: new Date().toISOString(), hasPhoto: false, categoryID: '', locationID: '' };
  if (mode === 'edit') { r = await api('/api/reports/' + arg); if (r.userID !== S.user.userID && !isAdmin()) return go('/'); }
  const edit = mode === 'edit';
  app.innerHTML = `<div class="panel mid"><h1 style="margin-top:0">${edit ? 'Edit report' : 'New report'}</h1>
  ${edit ? '' : `<div class="tabs"><button type="button" data-t="Lost" class="${r.type === 'Lost' ? 'on' : ''}">I lost something</button><button type="button" data-t="Found" class="${r.type === 'Found' ? 'on' : ''}">I found something</button></div>`}
  <form id="f"><label>Title</label><input name="title" required maxlength="200" value="${esc(r.title)}" placeholder="e.g. Black wallet">
  <label>Description</label><textarea name="description" rows="4" required placeholder="Describe the item and where exactly…">${esc(r.description)}</textarea>
  <div class="two"><div><label>Category</label><select name="categoryID" required>${opt([['', 'Select…'], ...S.cats.map(c => [c.categoryID, c.name])], r.categoryID)}</select></div>
  <div><label>Location</label><select name="locationID" required>${opt([['', 'Select…'], ...S.locs.map(l => [l.locationID, l.name])], r.locationID)}</select></div></div>
  <label id="dl">Date</label><input type="date" name="date" required max="${new Date().toISOString().slice(0, 10)}" value="${String(r.reportDate).slice(0, 10)}">
  <label>Photo (optional)</label>${edit && r.hasPhoto ? `<div class="ph prev" id="cur"><img src="${photoUrl(r.reportID)}" alt=""></div><label class="chk"><input type="checkbox" id="rmp"> Remove current photo</label>` : ''}
  <input type="file" name="photo" id="pf" accept="image/*"><div class="ph prev" id="pv" hidden></div>
  <p><button class="btn">${edit ? 'Save changes' : 'Submit report'}</button></p></form></div>`;
  const setLbl = () => $('#dl').textContent = r.type === 'Lost' ? 'Date lost' : 'Date found'; setLbl();
  app.querySelectorAll('[data-t]').forEach(b => b.onclick = () => { r.type = b.dataset.t; app.querySelectorAll('[data-t]').forEach(x => x.classList.toggle('on', x === b)); setLbl(); });
  $('#pf').onchange = e => {
    const f = e.target.files[0], pv = $('#pv');
    if (!f) { pv.hidden = true; return; }
    pv.hidden = false; pv.innerHTML = ''; const im = new Image(); im.src = URL.createObjectURL(f); pv.append(im);
  };
  $('#f').onsubmit = async e => {
    e.preventDefault(); const f = e.target;
    const body = { title: f.title.value.trim(), description: f.description.value.trim(), reportDate: f.date.value + 'T00:00:00', type: r.type, categoryID: +f.categoryID.value, locationID: +f.locationID.value };
    try {
      const file = f.photo.files[0], ready = file ? await prepImage(file) : null;   // validate the image before saving anything
      const saved = edit ? await api('/api/reports/' + arg, { method: 'PUT', body }) : await api('/api/reports', { method: 'POST', body });
      let note = edit ? 'Report updated' : 'Report published';
      try {
        if (ready) { const fdata = new FormData(); fdata.append('photo', ready); await api(`/api/reports/${saved.reportID}/photo`, { method: 'PUT', body: fdata }); }
        else if (edit && $('#rmp')?.checked) await api(`/api/reports/${saved.reportID}/photo`, { method: 'DELETE' });
      } catch (pe) { note = note + ', but the photo failed: ' + pe.message; }
      toast(note); go('/report/' + saved.reportID);
    } catch (er) { toast(er.message, 1); }
  };
}

function auth(kind) {
  const reg = kind === 'register';
  app.innerHTML = `<div class="panel narrow"><h1 style="margin-top:0">${reg ? 'Create account' : 'Welcome back'}</h1><form id="a">
  <label>Username</label><input name="username" required autocomplete="username">
  ${reg ? '<label>Email</label><input type="email" name="email" required><label>Phone (optional)</label><input name="phone">' : ''}
  <label>Password</label><input type="password" name="password" required autocomplete="${reg ? 'new' : 'current'}-password">
  <p><button class="btn" style="width:100%">${reg ? 'Sign up' : 'Log in'}</button></p></form>
  <p class="muted" style="text-align:center">${reg ? 'Already have an account? <a href="#/login">Log in</a>' : 'New here? <a href="#/register">Create an account</a>'}</p></div>`;
  $('#a').onsubmit = async e => {
    e.preventDefault(); const v = Object.fromEntries(new FormData(e.target));
    try {
      if (reg) { await api('/api/auth/register', { method: 'POST', body: v }); toast('Account created. Logging you in…'); }
      const res = await api('/api/auth/login', { method: 'POST', body: { username: v.username, password: v.password } });
      setAuth(res.token, res.user); go('/');
    } catch (er) { toast(er.message, 1); }
  };
}

async function me() {
  if (!S.user) return go('/login');
  const [reports, claims] = await Promise.all([api('/api/reports/my'), api('/api/claims/my')]);
  app.innerHTML = `<div class="panel"><b>${esc(S.user.username)}</b> · ${esc(S.user.email)} <span class="badge">${S.user.role}</span></div>
  <h2 style="margin-top:0">My reports</h2>${grid(reports, 'You have not posted any reports yet.')}
  <h2>My claims</h2>${claims.length ? `<div class="tbl"><table><tr><th>Item</th><th>Proof</th><th>Date</th><th>Status</th><th></th></tr>${claims.map(c => `<tr>
  <td><a href="#/report/${c.reportID}">Report #${c.reportID}</a></td><td>${esc(c.proof)}</td><td>${fd(c.createdDate)}</td>
  <td><span class="badge ${c.status}">${c.status}</span>${c.rejectionReason ? `<div class="muted">${esc(c.rejectionReason)}</div>` : ''}</td>
  <td>${c.status === 'Pending' ? `<button class="btn sm gray" data-c="${c.claimID}">Cancel</button>` : ''}</td></tr>`).join('')}</table></div>` : '<div class="empty">No claims yet.</div>'}`;
  app.querySelectorAll('[data-c]').forEach(b => b.onclick = async () => { await api(`/api/claims/${b.dataset.c}/cancel`, { method: 'PATCH' }); toast('Claim cancelled'); me(); });
}

let adminStatus = 'Pending';
async function admin() {
  if (!isAdmin()) return go('/');
  const claims = await api('/api/claims', { query: { status: adminStatus } });
  const box = (title, path, idk, list) => `<div class="panel"><h3 style="margin-top:0">${title}</h3>${list.map(x => `<span class="chip">${esc(x.name)}<button data-d="${path}/${x[idk]}" title="Delete">✕</button></span>`).join('')}
  <form data-add="${path}" class="row" style="margin-top:10px;justify-content:flex-start"><input name="name" required placeholder="Add new…" style="max-width:240px"><button class="btn sm">Add</button></form></div>`;
  app.innerHTML = `<h1 style="margin-top:0">Admin panel</h1><h2 style="margin-top:0">Claims</h2>
  <div class="tabs">${['Pending', 'Approved', 'Rejected', 'Cancelled'].map(s => `<button data-st="${s}" class="${s === adminStatus ? 'on' : ''}">${s}</button>`).join('')}</div>
  ${claims.length ? `<div class="tbl"><table><tr><th>#</th><th>Report</th><th>Proof</th><th>User</th><th>Date</th><th></th></tr>${claims.map(c => `<tr><td>${c.claimID}</td><td><a href="#/report/${c.reportID}">#${c.reportID}</a></td><td>${esc(c.proof)}</td><td>${c.userID}</td><td>${fd(c.createdDate)}</td>
  <td>${c.status === 'Pending' ? `<button class="btn sm" data-a="approve" data-id="${c.claimID}">Approve</button> <button class="btn sm danger" data-a="reject" data-id="${c.claimID}">Reject</button>` : `<button class="btn sm gray" data-a="reopen" data-id="${c.claimID}">Reopen</button>`}</td></tr>`).join('')}</table></div>` : `<div class="empty">No ${adminStatus.toLowerCase()} claims.</div>`}
  <h2>Categories &amp; locations</h2><div class="two">${box('Categories', '/api/categories', 'categoryID', S.cats)}${box('Locations', '/api/locations', 'locationID', S.locs)}</div>`;
  app.querySelectorAll('[data-st]').forEach(b => b.onclick = () => { adminStatus = b.dataset.st; admin().catch(e => toast(e.message, 1)); });
  app.querySelectorAll('[data-a]').forEach(b => b.onclick = async () => {
    const a = b.dataset.a, q = {};
    if (a === 'reject') { q.rejectionReason = prompt('Reason for rejection:'); if (!q.rejectionReason) return; }
    try { await api(`/api/claims/${b.dataset.id}/${a}`, { method: 'PATCH', query: q }); toast('Done'); admin(); } catch (er) { toast(er.message, 1); }
  });
  const reload = async () => { await loadLookups(); admin(); };
  app.querySelectorAll('[data-add]').forEach(f => f.onsubmit = async e => { e.preventDefault(); try { await api(f.dataset.add, { method: 'POST', body: { name: f.name.value } }); reload(); } catch (er) { toast(er.message, 1); } });
  app.querySelectorAll('[data-d]').forEach(b => b.onclick = async () => { if (confirm('Delete this item?')) try { await api(b.dataset.d, { method: 'DELETE' }); reload(); } catch (er) { toast(er.message, 1); } });
}

/* ---------- router ---------- */
async function route() {
  const [, a, b] = (location.hash.slice(1) || '/').split('/');
  window.scrollTo(0, 0); nav();
  try {
    if (!a) await home(); else if (a === 'report') await detail(+b); else if (a === 'new') await form('new', b);
    else if (a === 'edit') await form('edit', +b); else if (a === 'login' || a === 'register') auth(a);
    else if (a === 'me') await me(); else if (a === 'admin') await admin(); else app.innerHTML = '<div class="empty">Page not found.</div>';
  } catch (e) { app.innerHTML = `<div class="empty"><h2>Something went wrong</h2><p>${esc(e.message)}</p><a class="btn" href="#/">Back home</a></div>`; }
}
async function loadLookups() { [S.cats, S.locs] = await Promise.all([api('/api/categories'), api('/api/locations')]); }
window.addEventListener('hashchange', route);
loadLookups().catch(e => toast(e.message, 1)).finally(route);
