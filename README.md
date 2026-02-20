# open-rigs

This repository defines standard, shareable descriptions of experimental rigs used in the open-rigs ecosystem.

Its purpose is to provide a common layer between shared physical hardware, acquisition workflows, and experiment or rig repositories.

---

## Core repositories

`open-rigs` is part of a small set of tightly related core repositories:

- **open-rigs** – shared, versioned descriptions of experimental rigs, base pydantic schemas on which to build new experimental repositories.
- **[rig-template](https://github.com/open-rigs/rig-template)** – the primary entry point for creating new experiment repositories, using Copier
- **[acquisition](https://github.com/open-rigs/acquisition)** – Bonsai-based workflows and operators for hardware control and data acquisition

Each of these repositories are developed in tandem, and are dependent on each other. As development is ongoing, publised versions of each will be locked to each other, i.e. v0.1.0 of one repository is compatible with v0.1.0 of the other tw. 

In general, lab members and experimentalists will only encounter these repositories indirectly via the template and acquisition stack, rather than directly cloning and working with them directly.

---

## What this repository is

`open-rigs` provides:

- A **catalogue of reusable rig definitions** (hardware components, connections, capabilities)
- A **shared contract** that acquisition workflows and experiments (and later, analysis pipelines) can rely on
- A **reference implementation** for how rigs should be described in open-rigs projects

The repository is deliberately *not* experiment-specific and does not contain task logic or analysis code.

---

## What this repository is not

`open-rigs` is **not**:

- A place for experiment code
- A Bonsai workflow repository

Instead, it aims to describe rigs in a way that is reusable, extensible, machine-readable, and understandable by humans.

---

## Repository structure (conceptual)

While details may evolve, the repository generally contains:

- **Rig definitions** – structured descriptions of complete rigs
- **Component definitions** – cameras, DAQs, lasers, stages, etc.
- **Schemas** – formal validation of rig and component structure
- **Examples** – minimal, concrete rigs demonstrating best practice

The emphasis is on *composition*: rigs are built from components rather than defined monolithically.

---

## Using this repository

In most cases, you will **not interact with this repository directly**.

The most common usage is **automatic**, via:

- An experiment repository created from the [rig-template](https://github.com/open-rigs/rig-template)
- The template’s **Copier** configuration, which pulls in `open-rigs`
- Integration with the [acquisition](https://github.com/open-rigs/acquisition) repository

Rig definitions are added as a dependency, version-pinned by default, and ready to be selected or extended without manual setup.

You typically only work directly with this repository when adding reusable rigs or components, extending the shared catalogue, or updating schemas and validation rules.

If you find yourself copying files out of this repository, you are most likely doing something outside the intended framework.

---

## Extending rigs for your experiment

Users are encouraged to:

- Reuse existing components where possible
- Extend base rigs rather than duplicating them
- Keep local specialisation minimal and explicit

If a component or rig is generally useful, it should live here rather than in a single experiment repository. General practice will likely be that devices can be built locally in specific experimental repositories, where they can be explicitly tested before being adapted and integrated into the acqusition package.

---

## Versioning and dependency locking

Rig definitions are versioned and intended to be **locked by downstream repositories**.

Typical practice is:

- Experiment and acquisition repositories pin a specific version (tag or commit) of `open-rigs`
- Rig updates are pulled deliberately, not implicitly
- Breaking changes require an explicit version bump

This ensures experiments remain reproducible over time, hardware changes do not silently affect running code, and historical data can always be interpreted against the correct rig definition.

---