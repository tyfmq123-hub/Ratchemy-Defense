/** @typedef {Object} Fighter
 * @property {string} id
 * @property {string} name
 * @property {number} hp
 * @property {number} hitDamage
 * @property {number} attackSpeed
 * @property {number} moveSpeed
 * @property {number} attackRange
 * @property {number} damageReduction
 * @property {number} bonusSkillDps
 * @property {number} cost
 */

(function (global) {
  function cloneFighter(fighter) {
    return { ...fighter };
  }

  function baseDps(fighter) {
    return fighter.hitDamage * Math.max(fighter.attackSpeed, 0.01) + fighter.bonusSkillDps;
  }

  function getTeamCost(team) {
    return team.reduce((sum, unit) => sum + (unit.cost || 0), 0);
  }

  function buildTeamFromCounts(roster, counts) {
    const team = [];
    for (const unit of roster) {
      const count = counts[unit.id] || 0;
      for (let i = 0; i < count; i++) {
        team.push(cloneFighter(unit));
      }
    }
    return team;
  }

  function rollRandomPlayerTeam(roster, budget, rng) {
    const team = [];
    if (!roster.length || budget <= 0) return team;

    let minCost = Infinity;
    for (const unit of roster) {
      minCost = Math.min(minCost, Math.max(1, unit.cost || 1));
    }

    let remaining = budget;
    while (remaining >= minCost) {
      const affordable = roster.filter((unit) => unit.cost > 0 && unit.cost <= remaining);
      if (!affordable.length) break;
      const pick = affordable[Math.floor(rng() * affordable.length)];
      team.push(cloneFighter(pick));
      remaining -= pick.cost;
    }
    return team;
  }

  function rollRandomEnemyTeam(roster, count, rng) {
    const team = [];
    if (!roster.length || count <= 0) return team;
    for (let i = 0; i < count; i++) {
      const pick = roster[Math.floor(rng() * roster.length)];
      team.push(cloneFighter(pick));
    }
    return team;
  }

  function getLanePosition(index, count) {
    if (count <= 1) return 0;
    const spacing = 1.5;
    const center = (count - 1) * 0.5;
    return (index - center) * spacing;
  }

  function hasAliveSide(units, playerSide) {
    return units.some((unit) => unit.isPlayer === playerSide && unit.hp > 0);
  }

  function countAlive(units, playerSide) {
    return units.filter((unit) => unit.isPlayer === playerSide && unit.hp > 0).length;
  }

  function sumAliveHp(units, playerSide) {
    return units
      .filter((unit) => unit.isPlayer === playerSide && unit.hp > 0)
      .reduce((sum, unit) => sum + unit.hp, 0);
  }

  function findNearestEnemyIndex(units, selfIndex) {
    const self = units[selfIndex];
    let bestIndex = -1;
    let bestDistance = Infinity;

    for (let i = 0; i < units.length; i++) {
      if (i === selfIndex || units[i].hp <= 0 || units[i].isPlayer === self.isPlayer) continue;
      const dx = units[i].x - self.x;
      const dy = units[i].y - self.y;
      const distance = dx * dx + dy * dy;
      if (distance < bestDistance) {
        bestDistance = distance;
        bestIndex = i;
      }
    }
    return bestIndex;
  }

  function createBattleUnit(template, x, y, isPlayer) {
    return {
      template,
      x,
      y,
      hp: template.hp,
      attackCooldown: 0,
      isPlayer,
      spawnCost: Math.max(0, template.cost || 0),
      deathRefundGranted: false,
    };
  }

  function getMinPlayerCost(roster) {
    let minCost = Infinity;
    for (const unit of roster) minCost = Math.min(minCost, Math.max(1, unit.cost || 1));
    return minCost === Infinity ? Infinity : minCost;
  }

  function isPlayerSideDefeated(units, economy, costPool, playerRoster) {
    if (hasAliveSide(units, true)) return false;
    if (!economy?.enabled || !economy.autoReinforce || !playerRoster?.length) return true;
    return costPool + 0.001 < getMinPlayerCost(playerRoster);
  }

  function isBattleFinished(units, economy, costPool, playerRoster) {
    if (!hasAliveSide(units, false)) return true;
    return isPlayerSideDefeated(units, economy, costPool, playerRoster);
  }

  function processDeathRefunds(units, deathRefundRatio) {
    let refunded = 0;
    for (let i = 0; i < units.length; i++) {
      const unit = units[i];
      if (unit.hp > 0 || !unit.isPlayer || unit.deathRefundGranted) continue;
      const amount = unit.spawnCost * Math.min(Math.max(deathRefundRatio, 0), 1);
      unit.deathRefundGranted = true;
      refunded += amount;
    }
    return refunded;
  }

  function applyPassiveIncome(costState, incomePerSecond, dt) {
    costState.accumulator += incomePerSecond * dt;
    const wholeIncome = Math.floor(costState.accumulator);
    if (wholeIncome <= 0) return 0;
    costState.accumulator -= wholeIncome;
    costState.pool += wholeIncome;
    return wholeIncome;
  }

  function countDeadPlayerSlots(units) {
    return units.filter((unit) => unit.isPlayer && unit.hp <= 0).length;
  }

  function trySpawnReinforcements(units, playerRoster, costState, rng, costTracker) {
    let spawned = 0;
    while (true) {
      const affordable = playerRoster.filter((unit) => unit.cost > 0 && unit.cost <= costState.pool);
      if (!affordable.length) break;
      const pick = affordable[Math.floor(rng() * affordable.length)];
      const playerCount = countAlive(units, true) + countDeadPlayerSlots(units);
      units.push(createBattleUnit(pick, 0, getLanePosition(playerCount, playerCount + 1), true));
      costState.pool -= pick.cost;
      costTracker.total += pick.cost;
      spawned++;
    }
    return spawned;
  }

  function simulateTeamBattle(
    playerTeam,
    enemyTeam,
    startDistance = 10,
    maxDuration = 120,
    economy = null,
    playerRoster = null,
    rng = null
  ) {
    const units = [];
    const costState = { pool: 0, accumulator: 0 };
    const costTracker = { total: getTeamCost(playerTeam) };
    let reinforcementsSpawned = 0;
    let deathRefundsEarned = 0;
    let passiveIncomeEarned = 0;

    for (let i = 0; i < playerTeam.length; i++) {
      units.push(createBattleUnit(playerTeam[i], 0, getLanePosition(i, playerTeam.length), true));
    }

    for (let i = 0; i < enemyTeam.length; i++) {
      units.push(
        createBattleUnit(enemyTeam[i], Math.max(startDistance, 0.1), getLanePosition(i, enemyTeam.length), false)
      );
    }

    const dt = 0.05;
    let time = 0;

    while (time < maxDuration && !isBattleFinished(units, economy, costState.pool, playerRoster)) {
      for (let i = 0; i < units.length; i++) {
        if (units[i].hp <= 0) continue;

        const targetIndex = findNearestEnemyIndex(units, i);
        if (targetIndex < 0) continue;

        const unit = units[i];
        const target = units[targetIndex];
        const dx = target.x - unit.x;
        const dy = target.y - unit.y;
        const distance = Math.sqrt(dx * dx + dy * dy);
        const inRange = distance <= unit.template.attackRange;

        if (!inRange) {
          const move = unit.template.moveSpeed * dt;
          const safeDistance = Math.max(distance, 0.01);
          unit.x += (dx / safeDistance) * move;
          unit.y += (dy / safeDistance) * move;
        }

        unit.attackCooldown -= dt;

        if (inRange && unit.attackCooldown <= 0) {
          let damage = unit.template.hitDamage;
          if (!unit.isPlayer) {
            const dr = Math.min(Math.max(units[targetIndex].template.damageReduction, 0), 1);
            damage *= 1 - dr;
          }
          units[targetIndex].hp -= damage;
          unit.attackCooldown = 1 / Math.max(unit.template.attackSpeed, 0.01);
        }

        if (inRange && unit.template.bonusSkillDps > 0) {
          let bonus = unit.template.bonusSkillDps * dt;
          if (!unit.isPlayer) {
            const dr = Math.min(Math.max(units[targetIndex].template.damageReduction, 0), 1);
            bonus *= 1 - dr;
          }
          units[targetIndex].hp -= bonus;
        }
      }

      if (economy?.enabled) {
        const refund = processDeathRefunds(units, economy.deathRefundRatio ?? 0.5);
        costState.pool += refund;
        deathRefundsEarned += refund;
        passiveIncomeEarned += applyPassiveIncome(costState, economy.costIncomePerSecond ?? 1, dt);

        if (economy.autoReinforce && playerRoster?.length && rng) {
          reinforcementsSpawned += trySpawnReinforcements(units, playerRoster, costState, rng, costTracker);
        }
      }

      time += dt;
    }

    const playerAlive = countAlive(units, true);
    const enemyAlive = countAlive(units, false);
    const enemyDefeated = enemyAlive <= 0;
    const playerDefeated = isPlayerSideDefeated(units, economy, costState.pool, playerRoster);

    return {
      playerWin: playerAlive > 0 && enemyDefeated,
      draw: !enemyDefeated && !playerDefeated,
      duration: time,
      playerUnitsAlive: playerAlive,
      enemyUnitsAlive: enemyAlive,
      playerHpRemaining: sumAliveHp(units, true),
      enemyHpRemaining: sumAliveHp(units, false),
      playerCostSpent: costTracker.total,
      reinforcementsSpawned,
      deathRefundsEarned,
      passiveIncomeEarned,
    };
  }

  function createRng(seed) {
  let state = seed ? seed >>> 0 : Date.now() >>> 0;
  if (state === 0) state = 1;
  return () => {
    state = (1664525 * state + 1013904223) >>> 0;
    return state / 4294967296;
  };
}

  function runBatchSimulation(playerRoster, enemyRoster, options) {
  const {
    iterationCount,
    costBudget,
    enemyCount,
    startDistance,
    randomSeed = 0,
    useManualPlayerTeam = false,
    manualPlayerTeam = [],
    useManualEnemyTeam = false,
    manualEnemyTeam = [],
    economy = null,
  } = options;

  const rng = createRng(randomSeed || Date.now());
  const stats = {
    totalRuns: iterationCount,
    playerWins: 0,
    enemyWins: 0,
    draws: 0,
    avgDuration: 0,
    avgPlayerUnitCount: 0,
    avgEnemyUnitCount: 0,
    avgPlayerCostSpent: 0,
    avgReinforcements: 0,
    avgDeathRefunds: 0,
    avgPassiveIncome: 0,
    completed: 0,
  };

  let totalDuration = 0;
  let totalPlayerUnits = 0;
  let totalEnemyUnits = 0;
  let totalCostSpent = 0;
  let totalReinforcements = 0;
  let totalRefunds = 0;
  let totalPassiveIncome = 0;

  for (let run = 0; run < iterationCount; run++) {
    const playerTeam = useManualPlayerTeam
      ? manualPlayerTeam.map(cloneFighter)
      : rollRandomPlayerTeam(playerRoster, costBudget, rng);

    const enemyTeam = useManualEnemyTeam
      ? manualEnemyTeam.map(cloneFighter)
      : rollRandomEnemyTeam(enemyRoster, enemyCount, rng);

    if (!playerTeam.length || !enemyTeam.length) continue;

    const result = simulateTeamBattle(
      playerTeam,
      enemyTeam,
      startDistance,
      120,
      economy,
      playerRoster,
      rng
    );
    totalDuration += result.duration;
    totalPlayerUnits += playerTeam.length + result.reinforcementsSpawned;
    totalEnemyUnits += enemyTeam.length;
    totalCostSpent += result.playerCostSpent;
    totalReinforcements += result.reinforcementsSpawned;
    totalRefunds += result.deathRefundsEarned;
    totalPassiveIncome += result.passiveIncomeEarned;
    stats.completed++;

    if (result.draw) stats.draws++;
    else if (result.playerWin) stats.playerWins++;
    else stats.enemyWins++;
  }

  if (stats.completed > 0) {
    stats.avgDuration = totalDuration / stats.completed;
    stats.avgPlayerUnitCount = totalPlayerUnits / stats.completed;
    stats.avgEnemyUnitCount = totalEnemyUnits / stats.completed;
    stats.avgPlayerCostSpent = totalCostSpent / stats.completed;
    stats.avgReinforcements = totalReinforcements / stats.completed;
    stats.avgDeathRefunds = totalRefunds / stats.completed;
    stats.avgPassiveIncome = totalPassiveIncome / stats.completed;
  }

  return stats;
}

  function formatTeamSummary(team) {
  const counts = {};
  for (const unit of team) {
    counts[unit.name] = (counts[unit.name] || 0) + 1;
  }
  return (
    Object.entries(counts)
      .map(([name, count]) => (count > 1 ? `${name}x${count}` : name))
      .join(", ") || "(empty)"
  );
}

  function buildReportText({ playerRoster, enemyRoster, stats, settings, playerTeam, enemyTeam, singleResult }) {
  const lines = [];
  lines.push("=== Ratchemy Balance Web Simulator ===");
  lines.push(`exportedAt: ${settings.exportedAt || "unknown"}`);
  lines.push(`mode: ${settings.mode}`);
  lines.push(`costBudget: ${settings.costBudget}`);
  lines.push(`enemyCount(random): ${settings.enemyCount}`);
  lines.push(`startDistance: ${settings.startDistance}`);
  lines.push(`iterations: ${settings.iterationCount}`);
  if (settings.economy?.enabled) {
    lines.push(
      `economy: +${settings.economy.costIncomePerSecond}/s | refund ${Math.round((settings.economy.deathRefundRatio || 0) * 100)}% | autoReinforce ${settings.economy.autoReinforce ? "ON" : "OFF"}`
    );
  }
  lines.push("");

  if (settings.mode === "manual") {
    lines.push(`playerTeam: ${formatTeamSummary(playerTeam)}`);
    lines.push(`enemyTeam: ${formatTeamSummary(enemyTeam)}`);
    lines.push(`result: ${singleResult.draw ? "Draw" : singleResult.playerWin ? "PlayerWin" : "EnemyWin"}`);
    lines.push(`duration: ${singleResult.duration.toFixed(1)}s`);
    lines.push(`playerHpLeft: ${singleResult.playerHpRemaining.toFixed(0)}`);
    lines.push(`enemyHpLeft: ${singleResult.enemyHpRemaining.toFixed(0)}`);
  } else {
    const completed = stats.completed || 0;
    const winRate = completed > 0 ? ((stats.playerWins / completed) * 100).toFixed(1) : "0.0";
    lines.push(`completed: ${completed}`);
    lines.push(`playerWins: ${stats.playerWins} (${winRate}%)`);
    lines.push(`enemyWins: ${stats.enemyWins}`);
    lines.push(`draws: ${stats.draws}`);
    lines.push(`avgDuration: ${stats.avgDuration.toFixed(1)}s`);
    lines.push(`avgPlayerUnits: ${stats.avgPlayerUnitCount.toFixed(2)}`);
    lines.push(`avgCostSpent: ${stats.avgPlayerCostSpent.toFixed(1)}`);
    if (settings.economy?.enabled) {
      lines.push(`avgReinforcements: ${stats.avgReinforcements.toFixed(2)}`);
      lines.push(`avgDeathRefunds: ${stats.avgDeathRefunds.toFixed(1)}`);
      lines.push(`avgPassiveIncome: ${stats.avgPassiveIncome.toFixed(1)}`);
    }
  }

  lines.push("");
  lines.push("=== Player Roster ===");
  for (const unit of playerRoster) {
    lines.push(
      `${unit.name} | HP:${unit.hp} DMG:${unit.hitDamage} AS:${unit.attackSpeed} Range:${unit.attackRange} Cost:${unit.cost} DPS:${baseDps(unit).toFixed(1)}`
    );
  }

  lines.push("");
  lines.push("=== Enemy Pool ===");
  for (const unit of enemyRoster) {
    lines.push(
      `${unit.name} | HP:${unit.hp} DMG:${unit.hitDamage} AS:${unit.attackSpeed} Range:${unit.attackRange} DPS:${baseDps(unit).toFixed(1)}`
    );
  }

  return lines.join("\n");
  }

  global.BalanceSimEngine = {
    cloneFighter,
    baseDps,
    getTeamCost,
    buildTeamFromCounts,
    rollRandomPlayerTeam,
    rollRandomEnemyTeam,
    simulateTeamBattle,
    createRng,
    runBatchSimulation,
    formatTeamSummary,
    buildReportText,
  };
})(window);
