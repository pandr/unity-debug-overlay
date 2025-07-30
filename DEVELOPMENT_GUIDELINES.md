# Development Guidelines

This document contains guidelines extracted from analyzing preferred implementations to ensure future development aligns with the project's design philosophy.

## Core Principles

### 1. Prioritize Simplicity Over Abstraction
- **Prefer concrete classes over generic abstractions**
- **Avoid complex manager patterns when simple registration works**
- **Keep related functionality in single files when possible**
- **Write less code when possible**

### 2. Performance is Paramount
- **Direct field access over method calls for hot paths**
- **Minimize allocation and indirection**
- **Avoid implicit operators or complex conversion layers**
- **Prefer explicit over implicit behavior**

### 3. Follow Unity's Design Patterns
- **Use direct field access like Unity components do**
- **Keep registration simple and automatic**
- **Prefer concrete implementations over abstract interfaces**
- **Use clear, descriptive names**

### 4. Console Integration Should Feel Natural
- **Use intuitive command syntax (`name value` vs `name = value`)**
- **Query by name for getting values**
- **Keep parsing logic simple and direct**
- **Minimize complex assignment detection**

### 5. Code Style Guidelines
- **Write less code when possible**
- **Prefer concrete implementations over abstract interfaces**
- **Use clear, descriptive names**
- **Avoid over-engineering solutions**

### 6. File Organization
- **Keep related functionality together**
- **Prefer single files over multiple small files**
- **Minimize cross-file dependencies**

## Key Takeaways

1. **Simplicity wins**: The preferred implementation is much shorter and easier to understand
2. **Performance matters**: Direct field access (`cvar.value`) is faster than method calls
3. **Unity-like patterns**: Follow Unity's style of direct, explicit access
4. **Natural integration**: Console commands should feel intuitive and simple
5. **Less is more**: Avoid unnecessary abstraction layers and complex type systems

## When to Apply These Guidelines

- **New feature development**: Always consider the simpler approach first
- **Performance-critical code**: Prioritize direct access over abstraction
- **Console integration**: Keep commands natural and intuitive
- **Code reviews**: Question complex abstractions and prefer simpler solutions
- **Refactoring**: Look for opportunities to simplify existing code 