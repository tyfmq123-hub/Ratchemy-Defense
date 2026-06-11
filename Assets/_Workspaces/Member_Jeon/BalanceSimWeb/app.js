const {
  baseDps,
  buildReportText,
  buildTeamFromCounts,
  cloneFighter,
  createRng,
  getTeamCost,
  runBatchSimulation,
  simulateTeamBattle,
} = window.BalanceSimEngine;

const state = {
  rawData: null,
  playerRoster: [],
  enemyRoster: [],
  playerCounts: {},
  enemyCounts: {},
  playerOverrides: {},
  enemyOverrides: {},
  lastReport: "",
};

const els = {};

document.addEventListener("DOMContentLoaded", () => {
  if (!window.BalanceSimEngine) {
    document.getElementById("data-status").textContent =
      "스크립트 로드 실패 — index.html 과 같은 폴더에서 embedded-data.js, simulator.js, app.js 가 있는지 확인하세요.";
    document.getElementById("data-status").className = "status error";
    return;
  }

  cacheElements();
  bindEvents();
  loadDefaultData();
});

function cacheElements() {
  els.dataStatus = document.getElementById("data-status");
  els.jsonFile = document.getElementById("json-file");
  els.playerList = document.getElementById("player-list");
  els.enemyList = document.getElementById("enemy-list");
  els.playerCost = document.getElementById("player-cost");
  els.costBudget = document.getElementById("cost-budget");
  els.enemyCount = document.getElementById("enemy-count");
  els.startDistance = document.getElementById("start-distance");
  els.iterationCount = document.getElementById("iteration-count");
  els.randomSeed = document.getElementById("random-seed");
  els.enableEconomy = document.getElementById("enable-economy");
  els.costIncome = document.getElementById("cost-income");
  els.deathRefund = document.getElementById("death-refund");
  els.autoReinforce = document.getElementById("auto-reinforce");
  els.modeManual = document.getElementById("mode-manual");
  els.modeRandom = document.getElementById("mode-random");
  els.enemyModeManual = document.getElementById("enemy-mode-manual");
  els.enemyModeRandom = document.getElementById("enemy-mode-random");
  els.runButton = document.getElementById("run-button");
  els.resultBox = document.getElementById("result-box");
  els.downloadReport = document.getElementById("download-report");
  els.resetOverrides = document.getElementById("reset-overrides");
  els.clearTeams = document.getElementById("clear-teams");
}

function bindEvents() {
  els.jsonFile.addEventListener("change", onJsonFileSelected);
  els.runButton.addEventListener("click", runSimulation);
  els.downloadReport.addEventListener("click", downloadReport);
  els.resetOverrides.addEventListener("click", resetOverrides);
  els.clearTeams.addEventListener("click", clearTeams);
  els.costBudget.addEventListener("input", updatePlayerCostDisplay);
}

async function loadDefaultData() {
  const embedded = window.EMBEDDED_BALANCE_DATA;

  if (window.location.protocol === "file:") {
    if (embedded) {
      applyData(embedded, "내장 데이터");
      return;
    }
    setDataStatus("내장 데이터 없음 — JSON 파일 불러오기를 사용하세요.", true);
    return;
  }

  try {
    const response = await fetch("./data/balance-data.json");
    if (!response.ok) throw new Error("fetch failed");
    const data = await response.json();
    applyData(data, "data/balance-data.json");
  } catch {
    if (embedded) {
      applyData(embedded, "내장 데이터 (fallback)");
    } else {
      setDataStatus("JSON 자동 로드 실패 — 「JSON 파일 불러오기」를 사용하세요.", true);
    }
  }
}

function onJsonFileSelected(event) {
  const file = event.target.files?.[0];
  if (!file) return;

  const reader = new FileReader();
  reader.onload = () => {
    try {
      const data = JSON.parse(String(reader.result));
      applyData(data, file.name);
    } catch {
      setDataStatus("JSON 파싱 실패", true);
    }
  };
  reader.readAsText(file);
}

function applyData(data, sourceLabel) {
  state.rawData = data;
  state.playerRoster = (data.players || []).map(cloneFighter);
  state.enemyRoster = (data.enemies || []).map(cloneFighter);
  state.playerCounts = Object.fromEntries(state.playerRoster.map((unit) => [unit.id, 0]));
  state.enemyCounts = Object.fromEntries(state.enemyRoster.map((unit) => [unit.id, 0]));
  state.playerOverrides = {};
  state.enemyOverrides = {};

  renderUnitLists();
  updatePlayerCostDisplay();
  setDataStatus(`데이터 로드됨: ${sourceLabel} (${data.exportedAt || "no date"})`);
}

function setDataStatus(message, isError = false) {
  els.dataStatus.textContent = message;
  els.dataStatus.className = isError ? "status error" : "status ok";
}

function renderUnitLists() {
  els.playerList.innerHTML = "";
  els.enemyList.innerHTML = "";

  for (const unit of state.playerRoster) {
    els.playerList.appendChild(createUnitCard(unit, "player"));
  }

  for (const unit of state.enemyRoster) {
    els.enemyList.appendChild(createUnitCard(unit, "enemy"));
  }
}

function createUnitCard(baseUnit, side) {
  const card = document.createElement("article");
  card.className = "unit-card";

  const unit = getEffectiveUnit(baseUnit, side);
  const counts = side === "player" ? state.playerCounts : state.enemyCounts;

  card.innerHTML = `
    <header class="unit-header">
      <div>
        <h3>${unit.name}</h3>
        <p class="meta">Cost ${unit.cost || 0} · DPS ${baseDps(unit).toFixed(1)}</p>
      </div>
      <div class="count-controls" data-side="${side}" data-id="${unit.id}">
        <button type="button" class="count-btn" data-delta="-1">−</button>
        <span class="count-value">${counts[unit.id] || 0}</span>
        <button type="button" class="count-btn" data-delta="1">+</button>
      </div>
    </header>
    <details>
      <summary>스탯 조절</summary>
      <div class="stat-grid" data-side="${side}" data-id="${unit.id}">
        ${statField("hp", "HP", unit.hp, 1, 2000)}
        ${statField("hitDamage", "DMG", unit.hitDamage, 1, 200)}
        ${statField("attackSpeed", "AS", unit.attackSpeed, 0.1, 5, 0.1)}
        ${statField("moveSpeed", "Move", unit.moveSpeed, 0.1, 10, 0.1)}
        ${statField("attackRange", "Range", unit.attackRange, 0.1, 10, 0.1)}
        ${statField("bonusSkillDps", "Skill DPS", unit.bonusSkillDps, 0, 100, 0.1)}
        ${statField("damageReduction", "DR", unit.damageReduction, 0, 0.95, 0.01)}
      </div>
    </details>
  `;

  card.querySelectorAll(".count-btn").forEach((button) => {
    button.addEventListener("click", () => {
      const delta = Number(button.dataset.delta);
      changeCount(side, unit.id, delta);
    });
  });

  card.querySelectorAll(".stat-input").forEach((input) => {
    input.addEventListener("change", () => {
      setOverride(side, unit.id, input.dataset.field, Number(input.value));
      renderUnitLists();
      updatePlayerCostDisplay();
    });
  });

  return card;
}

function statField(field, label, value, min, max, step = 1) {
  return `
    <label>
      ${label}
      <input class="stat-input" type="number" min="${min}" max="${max}" step="${step}"
        data-field="${field}" value="${value}">
    </label>
  `;
}

function getOverrides(side) {
  return side === "player" ? state.playerOverrides : state.enemyOverrides;
}

function getEffectiveUnit(baseUnit, side) {
  const overrides = getOverrides(side)[baseUnit.id] || {};
  return { ...baseUnit, ...overrides };
}

function getEffectiveRoster(side) {
  const roster = side === "player" ? state.playerRoster : state.enemyRoster;
  return roster.map((unit) => getEffectiveUnit(unit, side));
}

function setOverride(side, id, field, value) {
  const bucket = getOverrides(side);
  if (!bucket[id]) bucket[id] = {};
  bucket[id][field] = value;
}

function changeCount(side, id, delta) {
  const counts = side === "player" ? state.playerCounts : state.enemyCounts;
  const next = Math.max(0, (counts[id] || 0) + delta);

  if (side === "player") {
    const roster = getEffectiveRoster("player");
    const unit = roster.find((entry) => entry.id === id);
    if (!unit) return;

    const currentCost = getTeamCost(buildTeamFromCounts(roster, state.playerCounts));
    const nextCost = currentCost + delta * unit.cost;
    const budget = Number(els.costBudget.value);

    if (delta > 0 && nextCost > budget) {
      flashResult("코스트 한도를 초과합니다.");
      return;
    }
  }

  counts[id] = next;
  renderUnitLists();
  updatePlayerCostDisplay();
}

function updatePlayerCostDisplay() {
  const roster = getEffectiveRoster("player");
  const team = buildTeamFromCounts(roster, state.playerCounts);
  const used = getTeamCost(team);
  const budget = Number(els.costBudget.value);
  els.playerCost.textContent = `${used} / ${budget}`;
  els.playerCost.className = used > budget ? "cost bad" : "cost ok";
}

function clearTeams() {
  for (const id of Object.keys(state.playerCounts)) state.playerCounts[id] = 0;
  for (const id of Object.keys(state.enemyCounts)) state.enemyCounts[id] = 0;
  renderUnitLists();
  updatePlayerCostDisplay();
}

function resetOverrides() {
  state.playerOverrides = {};
  state.enemyOverrides = {};
  renderUnitLists();
  updatePlayerCostDisplay();
}

function getEconomySettings() {
  if (!els.enableEconomy.checked) return null;
  return {
    enabled: true,
    costIncomePerSecond: Number(els.costIncome.value) || 0,
    deathRefundRatio: Number(els.deathRefund.value) || 0,
    autoReinforce: els.autoReinforce.checked,
  };
}

function runSimulation() {
  if (!state.playerRoster.length || !state.enemyRoster.length) {
    flashResult("유닛 데이터가 없습니다. JSON을 먼저 불러오세요.");
    return;
  }

  const playerRoster = getEffectiveRoster("player");
  const enemyRoster = getEffectiveRoster("enemy");
  const costBudget = Number(els.costBudget.value);
  const enemyCount = Number(els.enemyCount.value);
  const startDistance = Number(els.startDistance.value);
  const iterationCount = Number(els.iterationCount.value);
  const randomSeed = Number(els.randomSeed.value);
  const manualMode = els.modeManual.checked;
  const manualEnemy = els.enemyModeManual.checked;

  const playerTeam = buildTeamFromCounts(playerRoster, state.playerCounts);
  const enemyTeam = buildTeamFromCounts(enemyRoster, state.enemyCounts);

  if (manualMode && playerTeam.length === 0) {
    flashResult("아군 유닛을 1마리 이상 선택하세요.");
    return;
  }

  if (manualEnemy && enemyTeam.length === 0) {
    flashResult("적 유닛을 1마리 이상 선택하세요.");
    return;
  }

  if (manualMode && getTeamCost(playerTeam) > costBudget) {
    flashResult("아군 코스트가 한도를 초과했습니다.");
    return;
  }

  const economy = getEconomySettings();

  const settings = {
    exportedAt: state.rawData?.exportedAt,
    mode: manualMode && manualEnemy && iterationCount <= 1 ? "manual" : "batch",
    costBudget,
    enemyCount,
    startDistance,
    iterationCount,
    economy,
  };

  els.runButton.disabled = true;
  els.resultBox.textContent = "계산 중...";
  const rng = createRng(randomSeed || Date.now());

  setTimeout(() => {
    try {
      if (manualMode && manualEnemy && iterationCount <= 1) {
        const result = simulateTeamBattle(
          playerTeam,
          enemyTeam,
          startDistance,
          120,
          economy,
          playerRoster,
          rng
        );
        state.lastReport = buildReportText({
          playerRoster,
          enemyRoster,
          stats: null,
          settings: { ...settings, mode: "manual" },
          playerTeam,
          enemyTeam,
          singleResult: result,
        });

        const outcome = result.draw ? "무승부" : result.playerWin ? "아군 승리" : "적 승리";
        els.resultBox.textContent =
          `[1판 결과]\n` +
          `${outcome}\n` +
          `교전 시간: ${result.duration.toFixed(1)}s\n` +
          `아군 HP: ${result.playerHpRemaining.toFixed(0)} (생존 ${result.playerUnitsAlive}마리)\n` +
          `적 HP: ${result.enemyHpRemaining.toFixed(0)} (생존 ${result.enemyUnitsAlive}마리)\n` +
          (economy
            ? `재소환: ${result.reinforcementsSpawned} | 환급: ${result.deathRefundsEarned.toFixed(1)} | 패시브: ${result.passiveIncomeEarned.toFixed(1)}\n`
            : "") +
          `\n아군: ${playerTeam.map((u) => u.name).join(", ")}\n` +
          `적: ${enemyTeam.map((u) => u.name).join(", ")}`;
      } else {
        const stats = runBatchSimulation(playerRoster, enemyRoster, {
          iterationCount,
          costBudget,
          enemyCount,
          startDistance,
          randomSeed,
          useManualPlayerTeam: manualMode,
          manualPlayerTeam: playerTeam,
          useManualEnemyTeam: manualEnemy,
          manualEnemyTeam: enemyTeam,
          economy,
        });

        settings.mode = manualMode || manualEnemy ? "batch-fixed-team" : "batch-random";
        state.lastReport = buildReportText({
          playerRoster,
          enemyRoster,
          stats,
          settings,
          playerTeam: manualMode ? playerTeam : [],
          enemyTeam: manualEnemy ? enemyTeam : [],
          singleResult: null,
        });

        const completed = stats.completed || 0;
        const winRate = completed > 0 ? ((stats.playerWins / completed) * 100).toFixed(1) : "0.0";
        const teamNote = [];
        if (manualMode) teamNote.push("아군 고정");
        if (manualEnemy) teamNote.push("적 고정");
        if (!teamNote.length) teamNote.push("양쪽 랜덤");

        els.resultBox.textContent =
          `[${completed}판 / ${teamNote.join(" + ")}]\n` +
          `아군 승: ${stats.playerWins} (${winRate}%)\n` +
          `적 승: ${stats.enemyWins}\n` +
          `무승부: ${stats.draws}\n` +
          `평균 교전: ${stats.avgDuration.toFixed(1)}s\n` +
          `평균 아군 수: ${stats.avgPlayerUnitCount.toFixed(2)}마리\n` +
          `평균 코스트: ${stats.avgPlayerCostSpent.toFixed(1)} / ${costBudget}` +
          (economy
            ? `\n평균 재소환: ${stats.avgReinforcements.toFixed(2)} | 환급: ${stats.avgDeathRefunds.toFixed(1)} | 패시브: ${stats.avgPassiveIncome.toFixed(1)}`
            : "");
      }
    } catch (error) {
      els.resultBox.textContent = `오류: ${error.message}`;
    } finally {
      els.runButton.disabled = false;
    }
  }, 10);
}

function flashResult(message) {
  els.resultBox.textContent = message;
}

function downloadReport() {
  if (!state.lastReport) {
    flashResult("먼저 시뮬레이션을 실행하세요.");
    return;
  }

  const blob = new Blob([state.lastReport], { type: "text/plain;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = "BalanceReport.txt";
  link.click();
  URL.revokeObjectURL(url);
}
