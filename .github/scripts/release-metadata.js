module.exports = async ({ github, context, core, baseVersion }) => {
  const rank = {
    "semver:patch": 1,
    "semver:minor": 2,
    "semver:major": 3,
  };

  const releasedIssues = new Set();
  const mainPulls = await github.paginate(github.rest.pulls.list, {
    owner: context.repo.owner,
    repo: context.repo.repo,
    base: "main",
    state: "closed",
    sort: "updated",
    direction: "desc",
    per_page: 100,
  });

  for (const pull of mainPulls) {
    if (!pull.merged_at) {
      continue;
    }

    if (pull.head.ref.startsWith("release/v")) {
      const included = (pull.body || "").match(/Included Issues:\s*([0-9,\s]+)/i);
      if (included) {
        for (const value of included[1].split(",")) {
          const issueNumber = Number(value.trim());
          if (issueNumber) {
            releasedIssues.add(issueNumber);
          }
        }
      }
    }

    if (pull.head.ref.startsWith("hotfix/")) {
      for (const match of (pull.body || "").matchAll(/Issue:\s*#(\d+)/gi)) {
        releasedIssues.add(Number(match[1]));
      }
    }
  }

  const pulls = await github.paginate(github.rest.pulls.list, {
    owner: context.repo.owner,
    repo: context.repo.repo,
    base: "develop",
    state: "closed",
    sort: "updated",
    direction: "desc",
    per_page: 100,
  });

  const issueNumbers = new Set();
  for (const pull of pulls) {
    if (!pull.merged_at || !pull.head.ref.startsWith("feature/")) {
      continue;
    }

    const matches = [...(pull.body || "").matchAll(/Issue:\s*#(\d+)/gi)];
    const numbers = [...new Set(matches.map((match) => Number(match[1])))];
    if (numbers.length !== 1) {
      core.setFailed(`Merged PR #${pull.number} must contain exactly one 'Issue: #<number>' reference.`);
      return;
    }

    if (!releasedIssues.has(numbers[0])) {
      issueNumbers.add(numbers[0]);
    }
  }

  let selectedRank = 0;
  for (const issueNumber of issueNumbers) {
    const { data: issue } = await github.rest.issues.get({
      owner: context.repo.owner,
      repo: context.repo.repo,
      issue_number: issueNumber,
    });
    const semverLabels = issue.labels
      .map((label) => typeof label === "string" ? label : label.name)
      .filter((label) => Object.hasOwn(rank, label));
    if (semverLabels.length !== 1) {
      core.setFailed(`Issue #${issueNumber} must have exactly one semver label.`);
      return;
    }

    selectedRank = Math.max(selectedRank, rank[semverLabels[0]]);
  }

  const bump = ["None", "Patch", "Minor", "Major"][selectedRank];
  core.setOutput("base-version", baseVersion);
  core.setOutput("bump", bump);
  core.setOutput("issues", [...issueNumbers].sort((a, b) => a - b).join(","));
};
