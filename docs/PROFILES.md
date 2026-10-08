# Deployment profiles

ZDeployPet separates a shareable project contract from a private local binding.

- A project may commit `.zdeploypet/deployment.json` containing a relative runner path, logical target IDs, reporter shape and required capabilities.
- ZDeployPet stores actual project paths, WSL distribution, hosts, users and remote destinations under `%LOCALAPPDATA%\Zomniverse\ZDeployPet\profiles`.
- Passwords, passphrases, tokens and private-key contents are never profile fields.

The example under `examples/generic-wsl-script` is deliberately sanitized. It contains no real infrastructure or personal filesystem path.

## Validation

Profile saving requires an existing Windows project, a matching WSL-visible directory, a regular deployment script canonically contained below that project, at least one complete SSH target, and a safe absolute remote destination below the remote filesystem/home root. Live execution remains unavailable until a later phase proves a dry run.
