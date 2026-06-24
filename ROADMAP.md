# Civil 3D MCP — Roadmap Profesional

Plan de desarrollo para llevar el proyecto de demo funcional a **acceso profesional completo** a Civil 3D vía IA.

## Visión

La IA debe poder operar Civil 3D como un ingeniero civil avanzado: consultar el dibujo, crear y modificar objetos, ejecutar comandos nativos, exportar datos y manejar workflows largos con conexión robusta y errores claros.

## Arquitectura objetivo

```
┌─────────────────────────────────────────────────────────────┐
│  Cliente IA (Cursor / Claude)                               │
└───────────────────────────┬─────────────────────────────────┘
                            │ stdio MCP
┌───────────────────────────▼─────────────────────────────────┐
│  Servidor MCP (TypeScript)                                  │
│  • civil3d_health    • civil3d_query    • civil3d_execute          │
│  • civil3d_command • civil3d_session  • civil3d_discover          │
│  • civil3d_skills                                                 │
│  Conexión persistente + reconexión automática               │
└───────────────────────────┬─────────────────────────────────┘
                            │ TCP JSON-RPC (cola serializada)
┌───────────────────────────▼─────────────────────────────────┐
│  Plugin Civil 3D (C#)                                       │
│  Roslyn + ScriptContext + transacciones + comandos nativos  │
└───────────────────────────┬─────────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────────┐
│  Autodesk Civil 3D — API .NET + comandos nativos            │
└─────────────────────────────────────────────────────────────┘

* = fases futuras
```

---

## Fase A — Núcleo de conexión profesional

**Objetivo:** Base sólida, estable y observable. Sin esto, nada más es confiable en producción.

| # | Tarea | Estado |
|---|-------|--------|
| A1 | `civil3d_health` — tool MCP + info de versión C3D, dibujo, plugin | ✅ Completado |
| A2 | Cola serializada en plugin (una operación a la vez) | ✅ Completado |
| A3 | Puerto configurable (`CIVIL3D_PORT`) en plugin y servidor | ✅ Completado |
| A4 | Errores estructurados (código + datos + diagnósticos Roslyn) | ✅ Completado |
| A5 | Conexión MCP persistente con reconexión automática | ✅ Completado |

**Criterios de aceptación:**
- La IA puede llamar `civil3d_health` y ver versión de Civil 3D, dibujo activo y estado del listener.
- Dos llamadas concurrentes no corrompen el estado del dibujo.
- Plugin y servidor usan el mismo puerto vía variable de entorno.
- Errores de compilación C# incluyen diagnósticos legibles.
- El servidor MCP no abre/cierra TCP en cada llamada.

---

## Fase B — Acceso API ampliado

**Objetivo:** Desbloquear el 80% del trabajo civil real.

| # | Tarea | Estado |
|---|-------|--------|
| B1 | Ampliar `ScriptContext` (Application, CivilApplication, helpers) | ✅ Completado |
| B2 | `civil3d_command` — ejecutar comandos nativos (`SendStringToExecute`) | ✅ Completado |
| B3 | Regen / `UpdateScreen` automático tras operaciones de escritura | ✅ Completado |
| B4 | Serialización robusta de retornos (ObjectId → handle, Point3d → xyz) | ✅ Completado |
| B5 | Modo sesión — transacciones multi-paso con commit explícito | ✅ Completado |

**Criterios de aceptación:**
- La IA puede crear un corridor vía API o comando nativo.
- Los retornos JSON son consistentes y no fallan con tipos AutoCAD.
- Cambios visibles en pantalla sin regen manual.

---

## Fase C — Conocimiento civil profesional

**Objetivo:** Que la IA no adivine — que conozca los patrones del dominio.

| # | Tarea | Estado |
|---|-------|--------|
| C1 | Skills: corridors, pipe networks, parcels | ✅ Completado |
| C2 | Skills: profiles, sections, labels, estilos | ✅ Completado |
| C3 | `civil3d_discover` — inventario del dibujo sin escribir C# | ✅ Completado |
| C4 | Skills: LandXML, export CSV, quantity takeoff | ✅ Completado |
| C5 | Documentación de patrones en `skills/workflows/` | ✅ Completado |

---

## Fase D — Operaciones pesadas y producción

**Objetivo:** Soportar workflows reales de ingeniería (builds largos, múltiples usuarios).

| # | Tarea | Estado |
|---|-------|--------|
| D1 | Timeouts configurables por operación | ✅ Completado |
| D2 | Progress / status para operaciones largas | ✅ Completado |
| D3 | Setup automático: detectar versión C3D + build plugin | ✅ Completado |
| D4 | UI de configuración (opcional) | ✅ Completado |
| D5 | Audit log de operaciones ejecutadas por la IA | ✅ Completado |

---

## Fase E — Seguridad y políticas de IO

**Objetivo:** Balance entre acceso completo y seguridad controlada.

| # | Tarea | Estado |
|---|-------|--------|
| E1 | Política de exportación de archivos (carpetas permitidas) | ⬜ Pendiente |
| E2 | Sandbox configurable (estricto / profesional / desbloqueado) | ⬜ Pendiente |
| E3 | Confirmación opcional para operaciones destructivas | ⬜ Pendiente |

---

## Orden de implementación

```
Fase A (base)  →  Fase B (acceso)  →  Fase C (conocimiento)  →  Fase D (producción)  →  Fase E (seguridad)
     ↑
  EMPEZAMOS AQUÍ
```

## Leyenda de estados

| Símbolo | Significado |
|---------|-------------|
| ⬜ | Pendiente |
| 🔄 | En progreso |
| ✅ | Completado |

---

## Changelog

| Fecha | Fase | Cambio |
|-------|------|--------|
| 2026-06-24 | A | Creación del roadmap; inicio Fase A |
| 2026-06-24 | D | Fase D: timeouts, status, audit, setup.ps1 |
