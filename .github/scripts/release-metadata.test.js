const assert = require("node:assert/strict");
const collect = require("./release-metadata.js");

const outputs = new Map();
let failed;
const mainPulls = [
  {
    merged_at: "2026-01-01T00:00:00Z",
    head: { ref: "release/v1.0.0" },
    body: "Included Issues: 10,11",
  },
  {
    merged_at: "2026-01-02T00:00:00Z",
    head: { ref: "hotfix/12-fix" },
    body: "Issue: #12",
  },
];
const developPulls = [
  {
    number: 20,
    merged_at: "2026-01-03T00:00:00Z",
    head: { ref: "feature/10-old" },
    body: "Issue: #10",
  },
  {
    number: 21,
    merged_at: "2026-01-04T00:00:00Z",
    head: { ref: "feature/13-new" },
    body: "Issue: #13",
  },
  {
    number: 22,
    merged_at: "2026-01-05T00:00:00Z",
    head: { ref: "feature/14-breaking" },
    body: "Issue: #14",
  },
];

const github = {
  paginate: async (_method, options) => options.base === "main" ? mainPulls : developPulls,
  rest: {
    pulls: { list: Symbol("pulls.list") },
    issues: {
      get: async ({ issue_number }) => ({
        data: {
          labels: [{ name: issue_number === 14 ? "semver:major" : "semver:minor" }],
        },
      }),
    },
  },
};
const core = {
  setFailed: (message) => { failed = message; },
  setOutput: (name, value) => outputs.set(name, value),
};

(async () => {
  await collect({
    github,
    context: { repo: { owner: "owner", repo: "repo" } },
    core,
    baseVersion: "1.0.1",
  });

  assert.equal(failed, undefined);
  assert.equal(outputs.get("base-version"), "1.0.1");
  assert.equal(outputs.get("bump"), "Major");
  assert.equal(outputs.get("issues"), "13,14");
  console.log("Release metadata aggregation passed.");
})().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
