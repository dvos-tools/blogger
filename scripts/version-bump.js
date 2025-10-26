#!/usr/bin/env node

const fs = require('fs');
const { execSync } = require('child_process');

// Read package.json
const packageJson = JSON.parse(fs.readFileSync('package.json', 'utf8'));
const currentVersion = packageJson.version;

console.log(`Current version: ${currentVersion}`);

// Get commits since last tag
let lastTag;
try {
  lastTag = execSync('git describe --tags --abbrev=0', { encoding: 'utf8' }).trim();
  console.log(`Last tag: ${lastTag}`);
} catch (error) {
  console.log('No previous tags found');
  lastTag = null;
}

// Get commits since last tag (or all commits if no tag)
const since = lastTag ? `${lastTag}..HEAD` : '';
const commits = execSync(`git log ${since} --oneline --format="%s|%an|%ae"`, { encoding: 'utf8' })
  .split('\n')
  .filter(commit => commit.trim())
  .map(commit => {
    const [message, author, email] = commit.split('|');
    return { message, author, email };
  });

console.log(`Found ${commits.length} commits since last tag`);

// Custom version bump logic based on your rules
let versionBump = 'patch'; // default
let hasBreaking = false;
let hasFeatExclamation = false;
let hasFeat = false;

for (const commit of commits) {
  if (commit.message.includes('BREAKING CHANGE') || commit.message.includes('release:')) {
    hasBreaking = true;
    break;
  }
  if (commit.message.startsWith('feat!:')) {
    hasFeatExclamation = true;
  }
  if (commit.message.startsWith('feat:')) {
    hasFeat = true;
  }
}

if (hasBreaking) {
  versionBump = 'major';
} else if (hasFeatExclamation) {
  versionBump = 'minor';
} else if (hasFeat) {
  versionBump = 'patch';
}

console.log(`Version bump type: ${versionBump}`);

// Calculate new version
const [major, minor, patch] = currentVersion.split('.').map(Number);
let newVersion;

switch (versionBump) {
  case 'major':
    newVersion = `${major + 1}.0.0`;
    break;
  case 'minor':
    newVersion = `${major}.${minor + 1}.0`;
    break;
  case 'patch':
  default:
    newVersion = `${major}.${minor}.${patch + 1}`;
    break;
}

console.log(`New version: ${newVersion}`);

// Update package.json
packageJson.version = newVersion;
fs.writeFileSync('package.json', JSON.stringify(packageJson, null, 2) + '\n');

console.log(`✅ Updated package.json to version ${newVersion}`);

// Generate changelog entry
const changelogEntry = `## [${newVersion}] - ${new Date().toISOString().split('T')[0]}

${commits.map(commit => `- ${commit.message}`).join('\n')}
`;

// Read existing changelog
let changelog = '';
try {
  changelog = fs.readFileSync('CHANGELOG.md', 'utf8');
} catch (error) {
  changelog = '# Changelog\n\nAll notable changes to this project will be documented in this file.\n\n';
}

// Insert new entry after the header
const lines = changelog.split('\n');
const insertIndex = lines.findIndex(line => line.startsWith('## [')) || 2;
lines.splice(insertIndex, 0, changelogEntry);

fs.writeFileSync('CHANGELOG.md', lines.join('\n'));

console.log(`✅ Updated CHANGELOG.md`);

// Get the author of the most recent commit (the person who merged the PR)
const lastCommitAuthor = commits[0]?.author || 'GitHub Action';
const lastCommitEmail = commits[0]?.email || 'action@github.com';

console.log(`\n🚀 Version bump completed!`);
console.log('📝 CHANGELOG.md updated');
console.log('📦 package.json updated');
console.log('');
console.log('You can now create a tag and release manually when ready.');
console.log(`Commit will be made by: ${lastCommitAuthor} <${lastCommitEmail}>`);