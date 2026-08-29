// Daily Closing — Create/Edit pages.
// Drives the repeatable line-item rows (Tom Select pickers, add/remove, re-indexing so the posted
// collection names stay contiguous, re-hydration after a failed submit or on Edit), the live
// Adjusted Reading / Actual Cash preview, and the Supervisor/Admin co-sign flow that unlocks the
// Transaction Date field. Investor Expenses gets one dedicated section per investor (e.g. "Samer
// Expenses") rather than a single shared list with an Investor picker per row — see investorSections.
(function () {
    'use strict';

    function readJson(id, fallback) {
        var el = document.getElementById(id);
        if (!el) return fallback;
        try { return JSON.parse(el.textContent || 'null') || fallback; } catch (e) { return fallback; }
    }

    var receiversOptions = readJson('dc-receivers', []);
    var investorsOptions = readJson('dc-investors', []);
    var employeesOptions = readJson('dc-employees', []);
    var customersOptions = readJson('dc-customers', []);
    var paymentMethodOptions = [
        { id: 'Whish Payment', name: 'Whish Payment' },
        { id: 'Visa Card', name: 'Visa Card' }
    ];

    // Rows carried over from a failed submit (validation error / duplicate date), or pre-filled on
    // Edit — keyed by section key, each entry shaped like that section's row view model (e.g.
    // { ReceiverId, Amount, Note }), property names matching the posted field names. InvestorExpenses
    // is one flat list here (matching what's actually posted) even though it's rendered as several
    // per-investor sections — see the DOMContentLoaded handler below, which buckets it by InvestorId.
    var initialRows = readJson('dc-initial-rows', {});

    // Every select that shares a role (e.g. "employee" for both Advances and Deductions, "customer"
    // for both Credits and Cashbacks) must get its own copy of the option objects, never the same
    // object references — Tom Select mutates option objects internally for its own bookkeeping, so
    // two instances sharing one object would corrupt each other's state (that was the actual bug:
    // picking Mahmoud for an Advance could silently break his availability in a Deduction row, and
    // likewise a Customer shared between Credits and Cashbacks).
    function cloneOptions(list) {
        return list.map(function (o) { return { id: o.id, name: o.name }; });
    }

    var OPTION_SOURCES = {
        receiver: function () { return cloneOptions(receiversOptions); },
        employee: function () { return cloneOptions(employeesOptions); },
        customer: function () { return cloneOptions(customersOptions); },
        paymentMethod: function () { return cloneOptions(paymentMethodOptions); }
    };

    var PLACEHOLDERS = {
        receiver: 'Select receiver...',
        employee: 'Select employee...',
        customer: 'Select customer...',
        paymentMethod: 'Payment method...'
    };

    var SECTIONS = [
        { key: 'GeneralExpenses', containerId: 'generalExpensesRows', totalId: 'totalGeneralExpenses' },
        { key: 'Advances', containerId: 'advancesRows', totalId: 'totalAdvances' },
        { key: 'Deductions', containerId: 'deductionsRows', totalId: 'totalDeductions' },
        { key: 'Credits', containerId: 'creditsRows', totalId: 'totalCredits' },
        { key: 'Cashbacks', containerId: 'cashbacksRows', totalId: 'totalCashbacks' },
        { key: 'NonCashPayments', containerId: 'nonCashRows', totalId: 'totalNonCash' }
    ];

    // One "section" per investor — same posted key (InvestorExpenses) and row template as each
    // other, but its own container/total/Add-button and its own InvestorId baked into every row
    // instead of picked per-row.
    var investorSections = investorsOptions.map(function (inv) {
        return {
            key: 'InvestorExpenses',
            investorId: inv.id,
            containerId: 'investorExpenseRows-' + inv.id,
            totalId: 'totalInvestorExpense-' + inv.id
        };
    });

    // All DOM row-containers that share a posted list key — for every section except
    // InvestorExpenses this is just that one section's own container, but InvestorExpenses spans one
    // container per investor and they all post into the same flat list, so indices/removal/totals
    // need to treat them as one combined sequence.
    function containersForKey(key) {
        if (key === 'InvestorExpenses') {
            return investorSections
                .map(function (s) { return document.getElementById(s.containerId); })
                .filter(function (el) { return el; });
        }
        for (var i = 0; i < SECTIONS.length; i++) {
            if (SECTIONS[i].key === key) {
                var el = document.getElementById(SECTIONS[i].containerId);
                return el ? [el] : [];
            }
        }
        return [];
    }

    // Every select's Tom Select instance is tracked in a group keyed by section(+investor)+role —
    // once a value is picked in one row, it's removed from every other row's option list *within
    // that same group* (refreshGroup below). Advances and Deductions track separate groups, so
    // picking an employee for an Advance doesn't block them from also getting a Deduction row;
    // likewise each investor's own Receiver pool is independent of every other investor's.
    var exclusionGroups = {};

    function getGroup(section, role) {
        var key = section.key + (section.investorId ? (':inv' + section.investorId) : '') + ':' + role;
        if (!exclusionGroups[key]) exclusionGroups[key] = { role: role, instances: [] };
        return exclusionGroups[key];
    }

    function refreshGroup(group) {
        var master = OPTION_SOURCES[group.role]();
        var selectedIds = group.instances.map(function (ts) { return ts.getValue(); }).filter(function (v) { return v; });
        group.instances.forEach(function (ts) {
            var own = ts.getValue();
            ts.clearOptions();
            master.forEach(function (opt) {
                if (opt.id === own || selectedIds.indexOf(opt.id) === -1) ts.addOption(opt);
            });
            ts.refreshOptions(false);
            if (own) ts.setValue(own, true); // silent — avoid re-triggering 'change'
        });
    }

    function fmt(n) { return '$' + (n || 0).toFixed(2); }

    function sumRowAmounts(containerId) {
        var els = document.querySelectorAll('#' + containerId + ' .row-amount');
        var total = 0;
        for (var i = 0; i < els.length; i++) total += (parseFloat(els[i].value) || 0);
        return total;
    }

    function recalc() {
        var mainReading = parseFloat(document.getElementById('mainReading').value) || 0;
        var totals = {};
        SECTIONS.forEach(function (s) {
            var t = sumRowAmounts(s.containerId);
            totals[s.key] = t;
            var el = document.getElementById(s.totalId);
            if (el) el.textContent = fmt(t);
        });

        var totalInvestorExpenses = 0;
        investorSections.forEach(function (s) {
            var t = sumRowAmounts(s.containerId);
            totalInvestorExpenses += t;
            var el = document.getElementById(s.totalId);
            if (el) el.textContent = fmt(t);
        });

        var adjusted = mainReading - totals.Credits + totals.Cashbacks;
        var fivePercent = adjusted * 0.05;
        var actualCash = adjusted - totals.GeneralExpenses - totalInvestorExpenses - totals.Advances - totals.NonCashPayments;

        document.getElementById('adjustedReadingDisplay').textContent = fmt(adjusted);
        document.getElementById('fivePercentDisplay').textContent = fmt(fivePercent);
        document.getElementById('actualCashDisplay').textContent = fmt(actualCash);
    }

    function initRowSelects(row, section) {
        var groups = [];
        var selects = row.querySelectorAll('select[data-role]');
        selects.forEach(function (sel) {
            var role = sel.dataset.role;
            var settings = {
                options: OPTION_SOURCES[role](),
                valueField: 'id',
                labelField: 'name',
                searchField: ['name'],
                placeholder: PLACEHOLDERS[role] || 'Select...',
                maxOptions: null
            };
            if (role === 'paymentMethod') {
                settings.create = function (input) {
                    var created = { id: input, name: input };
                    if (!paymentMethodOptions.some(function (o) { return o.id === created.id; })) {
                        paymentMethodOptions.push(created);
                    }
                    return created;
                };
                settings.createOnBlur = true;
            }
            var ts = new TomSelect(sel, settings);
            sel._tomSelect = ts;

            var group = getGroup(section, role);
            group.instances.push(ts);
            sel._exclusionGroup = group;
            ts.on('change', function () { refreshGroup(group); recalc(); });
            groups.push(group);
        });
        return groups;
    }

    // Id-based roles use 0 as "nothing picked" (matches the server's [Range(1, int.MaxValue)] rule) —
    // re-hydrating a row that was rejected for exactly that reason must leave the select empty, not
    // fabricate a fake option labeled "0" that looks like a real (wrong) selection.
    var ID_ROLES = { receiver: true, employee: true, customer: true };

    // Pushes a failed-submit (or Edit pre-fill) row's values back into a freshly-built row: matches
    // each field by the last segment of its posted name (e.g. "GeneralExpenses[0].ReceiverId" -> "ReceiverId").
    function fillRowFromData(row, data) {
        if (!data) return;
        row.querySelectorAll('[name]').forEach(function (el) {
            var parts = el.name.split('.');
            var field = parts[parts.length - 1];
            if (!(field in data)) return;
            var value = data[field];
            if (value === null || value === undefined) return;

            if (el.tagName === 'SELECT') {
                var ts = el._tomSelect;
                if (!ts) return;
                var role = el.dataset.role;
                var strValue = String(value);
                if (ID_ROLES[role] && strValue === '0') return; // nothing was actually picked — leave it empty

                if (!ts.options[strValue]) ts.addOption({ id: strValue, name: strValue });
                if (role === 'paymentMethod' && !paymentMethodOptions.some(function (o) { return o.id === strValue; })) {
                    paymentMethodOptions.push({ id: strValue, name: strValue });
                }
                ts.setValue(strValue, true);
            } else {
                el.value = value;
            }
        });
    }

    function reindexRows(section) {
        var prefixPattern = new RegExp('^' + section.key + '\\[\\d+\\]');
        var idx = 0;
        containersForKey(section.key).forEach(function (container) {
            Array.prototype.forEach.call(container.children, function (row) {
                row.querySelectorAll('[name]').forEach(function (el) {
                    el.name = el.name.replace(prefixPattern, section.key + '[' + idx + ']');
                });
                idx++;
            });
        });
    }

    function addRow(section, data) {
        var container = document.getElementById(section.containerId);
        var tpl = document.getElementById('tpl-' + section.key);

        var index = 0;
        containersForKey(section.key).forEach(function (c) { index += c.children.length; });

        var html = tpl.innerHTML.split('__INDEX__').join(String(index));
        if (section.investorId) html = html.split('__INVESTORID__').join(String(section.investorId));

        var wrapper = document.createElement('div');
        wrapper.innerHTML = html.trim();
        var row = wrapper.firstElementChild;
        container.appendChild(row);

        var groups = initRowSelects(row, section);
        fillRowFromData(row, data);

        row.querySelector('.remove-row').addEventListener('click', function () {
            removeRow(row, section);
        });
        row.querySelectorAll('.row-amount').forEach(function (input) {
            input.addEventListener('input', recalc);
        });

        groups.forEach(refreshGroup);
        recalc();
    }

    function removeRow(row, section) {
        var groups = [];
        row.querySelectorAll('select[data-role]').forEach(function (sel) {
            var ts = sel._tomSelect;
            if (!ts) return;
            var group = sel._exclusionGroup;
            if (group) {
                group.instances = group.instances.filter(function (x) { return x !== ts; });
                groups.push(group);
            }
            ts.destroy();
        });
        row.remove();
        reindexRows(section);
        groups.forEach(refreshGroup);
        recalc();
    }

    // A row nobody touched (no select chosen, no amount typed) is dropped silently rather than
    // blocking submit — that's just an unused "+ Add" click, not a mistake. A row with *some* data
    // but a missing pick or a non-positive amount is a real mistake: without this, it posts an Id of
    // 0 (server-side [Range] now rejects that, but the round trip is a bad experience — the whole
    // point of catching it here is to never make the user wait for it) — so submit is blocked and
    // every offending row is highlighted instead.
    function validateAndCleanBeforeSubmit() {
        var firstInvalid = null;
        var anyInvalid = false;

        SECTIONS.concat(investorSections).forEach(function (section) {
            var container = document.getElementById(section.containerId);
            if (!container) return;
            var rows = Array.prototype.slice.call(container.children);

            rows.forEach(function (row) {
                var selects = Array.prototype.slice.call(row.querySelectorAll('select[data-role]'));
                var amountInput = row.querySelector('.row-amount');
                var amount = parseFloat(amountInput.value) || 0;
                var selectValues = selects.map(function (sel) { return sel._tomSelect ? sel._tomSelect.getValue() : sel.value; });
                var noteInput = row.querySelector('input[type="text"]');
                var noteFilled = !!(noteInput && noteInput.value.trim());

                var untouched = amount === 0 && !noteFilled && selectValues.every(function (v) { return !v; });
                if (untouched) {
                    removeRow(row, section);
                    return;
                }

                var missingSelect = null;
                for (var i = 0; i < selects.length; i++) {
                    if (!selectValues[i]) { missingSelect = selects[i]; break; }
                }

                if (missingSelect || amount <= 0) {
                    row.classList.add('ring-2', 'ring-status-urgent');
                    anyInvalid = true;
                    if (!firstInvalid) firstInvalid = missingSelect || amountInput;
                } else {
                    row.classList.remove('ring-2', 'ring-status-urgent');
                }
            });
        });

        var banner = document.getElementById('rowValidationError');
        if (banner) banner.classList.toggle('hidden', !anyInvalid);

        if (firstInvalid) {
            var target = firstInvalid._tomSelect ? firstInvalid._tomSelect.control : firstInvalid;
            target.scrollIntoView({ behavior: 'smooth', block: 'center' });
            if (firstInvalid._tomSelect) firstInvalid._tomSelect.focus(); else firstInvalid.focus();
        }

        recalc(); // totals shift if any untouched rows just got dropped
        return !anyInvalid;
    }

    function wireUnlock() {
        var dateInput = document.getElementById('closeDate');
        var editBtn = document.getElementById('editDateBtn');
        var lockBtn = document.getElementById('lockDateBtn');
        var lockIcon = document.getElementById('dateLockIcon');
        var modalEl = document.getElementById('unlockModal');
        var unlockError = document.getElementById('unlockError');
        var usernameInput = document.getElementById('unlockUsername');
        var passwordInput = document.getElementById('unlockPassword');
        var submitBtn = document.getElementById('btnSubmitUnlock');

        function getModal() {
            return window.bootstrap ? bootstrap.Modal.getOrCreateInstance(modalEl) : null;
        }

        submitBtn.addEventListener('click', function () {
            unlockError.classList.add('hidden');
            var username = usernameInput.value.trim();
            var password = passwordInput.value;
            if (!username || !password) return;

            submitBtn.disabled = true;
            fetch(window.DAILY_CLOSE_CONFIG.verifyUnlockUrl, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                credentials: 'same-origin',
                body: JSON.stringify({ username: username, password: password })
            })
                .then(function (res) { return res.json(); })
                .then(function (data) {
                    submitBtn.disabled = false;
                    if (data && data.ok) {
                        dateInput.readOnly = false;
                        dateInput.focus();
                        lockIcon.textContent = 'lock_open';
                        editBtn.classList.add('hidden');
                        lockBtn.classList.remove('hidden');
                        usernameInput.value = '';
                        passwordInput.value = '';
                        var modal = getModal();
                        if (modal) modal.hide();
                    } else {
                        unlockError.classList.remove('hidden');
                    }
                })
                .catch(function () {
                    submitBtn.disabled = false;
                    unlockError.classList.remove('hidden');
                });
        });

        lockBtn.addEventListener('click', function () {
            dateInput.readOnly = true;
            lockIcon.textContent = 'lock';
            lockBtn.classList.add('hidden');
            editBtn.classList.remove('hidden');
        });
    }

    // Quick-add Employee/Customer/Receiver from inside the form — see the "+" buttons next to each
    // picker (data-quick-add="employee|customer|receiver") and _QuickAddModal.cshtml. Adding one
    // pushes it straight into that role's shared option pool (receiversOptions/employeesOptions/
    // customersOptions — the same arrays OPTION_SOURCES reads from) and refreshes every open picker
    // of that role immediately, no page reload — including selecting the new option in the exact row
    // that asked for it.
    var ROLE_CONFIG = {
        receiver: {
            title: 'Add Receiver',
            endpoint: '/api/daily-closing/quick-add/receiver',
            options: function () { return receiversOptions; },
            showPhone: false,
            showPosition: false,
            showBaseSalary: false,
            buildPayload: function (f) { return { name: f.name }; }
        },
        employee: {
            title: 'Add Employee',
            endpoint: '/api/daily-closing/quick-add/employee',
            options: function () { return employeesOptions; },
            showPhone: true,
            showPosition: true,
            showBaseSalary: true,
            buildPayload: function (f) {
                return { name: f.name, phoneNumber: f.phone || null, position: f.position || null, baseSalary: f.baseSalary };
            }
        },
        customer: {
            title: 'Add Customer',
            endpoint: '/api/daily-closing/quick-add/customer',
            options: function () { return customersOptions; },
            showPhone: true,
            showPosition: false,
            showBaseSalary: false,
            buildPayload: function (f) { return { name: f.name, phoneNumber: f.phone || null }; }
        }
    };

    function wireQuickAdd() {
        var modalEl = document.getElementById('quickAddModal');
        if (!modalEl) return;

        var titleEl = document.getElementById('quickAddModalTitle');
        var nameInput = document.getElementById('quickAddName');
        var phoneInput = document.getElementById('quickAddPhone');
        var phoneGroup = document.getElementById('quickAddPhoneGroup');
        var positionInput = document.getElementById('quickAddPosition');
        var positionGroup = document.getElementById('quickAddPositionGroup');
        var baseSalaryInput = document.getElementById('quickAddBaseSalary');
        var baseSalaryGroup = document.getElementById('quickAddBaseSalaryGroup');
        var errorBox = document.getElementById('quickAddError');
        var errorText = document.getElementById('quickAddErrorText');
        var submitBtn = document.getElementById('quickAddSubmitBtn');

        var currentRole = null;
        var currentSelect = null; // the <select> in the row whose "+" was clicked

        function getModal() {
            return window.bootstrap ? bootstrap.Modal.getOrCreateInstance(modalEl) : null;
        }

        document.addEventListener('click', function (e) {
            var btn = e.target.closest('.quick-add-btn');
            if (!btn) return;

            currentRole = btn.dataset.quickAdd;
            var config = ROLE_CONFIG[currentRole];
            if (!config) return;
            var row = btn.closest('.row-item');
            currentSelect = row ? row.querySelector('select[data-role="' + currentRole + '"]') : null;

            titleEl.textContent = config.title;
            nameInput.value = '';
            phoneInput.value = '';
            positionInput.value = '';
            baseSalaryInput.value = '';
            phoneGroup.style.display = config.showPhone ? '' : 'none';
            positionGroup.style.display = config.showPosition ? '' : 'none';
            baseSalaryGroup.style.display = config.showBaseSalary ? '' : 'none';
            errorBox.classList.add('hidden');

            var modal = getModal();
            if (modal) modal.show();
            setTimeout(function () { nameInput.focus(); }, 300);
        });

        submitBtn.addEventListener('click', function () {
            var config = ROLE_CONFIG[currentRole];
            if (!config) return;

            errorBox.classList.add('hidden');
            var name = nameInput.value.trim();
            if (!name) {
                errorText.textContent = 'Name is required.';
                errorBox.classList.remove('hidden');
                return;
            }
            var baseSalary = parseFloat(baseSalaryInput.value) || 0;
            if (config.showBaseSalary && baseSalary <= 0) {
                errorText.textContent = 'Base Salary is required and must be greater than 0.';
                errorBox.classList.remove('hidden');
                return;
            }

            var payload = config.buildPayload({
                name: name,
                phone: phoneInput.value.trim(),
                position: positionInput.value.trim(),
                baseSalary: baseSalary
            });

            submitBtn.disabled = true;
            fetch(config.endpoint, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                credentials: 'same-origin',
                body: JSON.stringify(payload)
            })
                .then(function (res) { return res.json(); })
                .then(function (data) {
                    submitBtn.disabled = false;
                    if (!data || data.status !== 'success') {
                        errorText.textContent = (data && data.message) || 'Could not add — please try again.';
                        errorBox.classList.remove('hidden');
                        return;
                    }

                    var newOption = { id: String(data.id), name: name };
                    config.options().push(newOption);

                    // Every open picker of this role (across every section/investor) gets the new
                    // option immediately, not just the row that asked for it.
                    Object.keys(exclusionGroups).forEach(function (key) {
                        var group = exclusionGroups[key];
                        if (group.role === currentRole) refreshGroup(group);
                    });

                    if (currentSelect && currentSelect._tomSelect) {
                        currentSelect._tomSelect.setValue(newOption.id); // fires 'change' -> refreshGroup + recalc
                    }

                    var modal = getModal();
                    if (modal) modal.hide();
                })
                .catch(function () {
                    submitBtn.disabled = false;
                    errorText.textContent = 'Network error — please try again.';
                    errorBox.classList.remove('hidden');
                });
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        SECTIONS.forEach(function (section) {
            document.querySelectorAll('[data-add-section="' + section.key + '"]').forEach(function (btn) {
                btn.addEventListener('click', function () { addRow(section); });
            });

            var rows = initialRows[section.key];
            if (rows && rows.length) {
                rows.forEach(function (rowData) { addRow(section, rowData); });
            }
            // No else: a fresh page (or a section with nothing to re-hydrate) starts with zero rows —
            // only "+ Add" creates one.
        });

        // Investor Expenses: the posted data is one flat InvestorExpenses list — bucket it by
        // InvestorId so each investor's section only pre-fills with its own rows.
        var investorRowsByInvestor = {};
        (initialRows.InvestorExpenses || []).forEach(function (r) {
            var key = String(r.InvestorId);
            (investorRowsByInvestor[key] = investorRowsByInvestor[key] || []).push(r);
        });

        investorSections.forEach(function (section) {
            document.querySelectorAll('[data-add-investor="' + section.investorId + '"]').forEach(function (btn) {
                btn.addEventListener('click', function () { addRow(section); });
            });

            var rows = investorRowsByInvestor[section.investorId];
            if (rows && rows.length) {
                rows.forEach(function (rowData) { addRow(section, rowData); });
            }
            // No else — see the SECTIONS loop above: an investor with nothing to re-hydrate also
            // starts with zero rows.
        });

        document.getElementById('mainReading').addEventListener('input', recalc);

        document.getElementById('clearForm').addEventListener('click', function () {
            window.location.reload();
        });

        document.getElementById('dailyCloseForm').addEventListener('submit', function (e) {
            if (!validateAndCleanBeforeSubmit()) e.preventDefault();
        });

        wireUnlock();
        wireQuickAdd();
        recalc();
    });
})();
