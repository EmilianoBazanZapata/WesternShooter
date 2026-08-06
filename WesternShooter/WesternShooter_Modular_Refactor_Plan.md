# WesternShooter — Plan de refactor a monolito modular

## 1. Objetivo

Transformar el código actual de WesternShooter en un **monolito modular pragmático para Unity 6**, organizado por funcionalidades y vertical slices, sin alterar el gameplay existente ni romper:

- escenas;
- prefabs;
- referencias del Inspector;
- Animator Controllers y parámetros;
- Input actual;
- Audio Mixer y snapshots;
- NavMesh;
- tiempos de ataque, disparo, spawn, victoria y derrota;
- métodos públicos usados por UnityEvents;
- archivos `.meta` y GUID de scripts.

El refactor no intentará convertir Unity en una API ni introducir patrones de backend de manera artificial. Se usarán los principios útiles: cohesión por feature, límites explícitos, dependencias unidireccionales, contratos públicos pequeños y comunicación mediante eventos cuando se anuncien hechos ocurridos.

---

## 2. Estado inicial observado

Los principales scripts que concentran responsabilidades son:

- `Character/PlayerMovementStateMachine.cs`
- `Enemy/ZombieStateMachine.cs`
- `Enemy/ZombieSpawnerManager.cs`
- `Audio/GameAudioManager.cs`
- `Core/GameManager.cs`
- `Camera/ThirdPersonCameraFollow.cs`

Dependencias directas problemáticas actuales:

- El jugador conoce `GameAudioManager` y `GameManager`.
- El spawner conoce `GameAudioManager` y `GameManager`.
- El HUD consulta cada frame al jugador y al spawner.
- La cámara, el jugador y el arma interpretan input por caminos diferentes.
- Los zombies buscan globalmente al jugador.
- Algunos triggers buscan servicios globalmente como fallback.
- `GameAudioManager` concentra mixer, snapshots, música, voz y pausa.

Problemas funcionales a estabilizar antes de cambios grandes:

- El flujo de `ZombieSpawnerManager.ProcessSpawnQueue()` tiene `if` anidados sin llaves y una indentación engañosa.
- `PlayerMovementStateMachine.TakeDamage()` acepta daño negativo y puede aumentar la vida.
- El daño del zombie ocurre al iniciar el ataque; esto se preservará durante el refactor.
- El evento de muerte del zombie se publica antes de terminar la animación; esto también se preservará inicialmente.
- `PlayerVoiceAudioController` puede crear varias coroutines para una misma fuente y desbalancear solicitudes de snapshot.
- `BulletProjectile` no está preparado para pooling porque no reinicia todo su estado al habilitarse.

---

## 3. Arquitectura objetivo

```text
Assets/Scripts/WesternShooter
├── Shared
│   ├── Contracts
│   └── Gameplay
├── Combat
│   ├── Damage
│   └── Projectiles
├── Player
│   ├── Core
│   ├── Input
│   ├── Movement
│   ├── Health
│   ├── Combat
│   └── Presentation
├── Zombies
│   ├── Core
│   ├── States
│   ├── Targeting
│   ├── Navigation
│   ├── Health
│   ├── Combat
│   └── Presentation
├── Spawning
│   ├── Core
│   ├── Progression
│   └── Triggers
├── GameFlow
│   ├── Core
│   └── States
├── Audio
│   ├── Mixer
│   ├── Music
│   ├── Snapshots
│   ├── Voice
│   └── World
├── Camera
└── UI
```

Esta estructura es el destino, no el primer commit. Los archivos se moverán solamente cuando las responsabilidades ya estén estabilizadas y siempre conservando sus `.meta`.

---

## 4. Reglas de dependencia

### Dependencias permitidas

```text
Shared      <- todos pueden depender de Shared
Combat      -> Shared
Player      -> Shared, Combat
Zombies     -> Shared, Combat
Spawning    -> Shared, Zombies
Audio       -> Shared
Camera      -> Player
GameFlow    -> Player, Spawning, Audio
UI          -> Player, Spawning, GameFlow, Audio
```

### Dependencias que deben desaparecer

```text
Player   -> UI
Player   -> GameFlow
Player   -> GameAudioManager
Zombies  -> UI
Zombies  -> GameFlow
Spawner  -> UI
Spawner  -> GameAudioManager
Audio    -> PlayerMovementStateMachine
```

### Regla de comunicación

- Una **orden inmediata** se representa con un método o una interfaz:
  - `TakeDamage`
  - `StartSpawning`
  - `SetTarget`
  - `ShowVictory`
- Un **hecho que ya ocurrió** se representa con un evento:
  - `HealthChanged`
  - `Died`
  - `TotalKilledChanged`
  - `VictoryThresholdReached`
  - `GameStateChanged`
  - `AimChanged`

No se introducirá inicialmente:

- EventBus global;
- MediatR;
- framework de inyección de dependencias;
- service locator;
- máquina de estados genérica;
- ScriptableObject Event Channel para cada evento pequeño.

Los eventos C# locales serán la opción predeterminada. Los Event Channels se evaluarán solamente para eventos de alto nivel que deban atravesar escenas o ser consumidos por varios módulos sin una relación directa clara.

---

# 5. Plan por fases

## Fase 0 — Resguardo y baseline

### Objetivo

Crear un punto de recuperación verificable y documentar el comportamiento actual antes de modificar código.

### Acciones

1. Crear una rama:
   - `refactor/modular-monolith`
2. Crear un tag o commit:
   - `baseline/pre-modular-refactor`
3. Confirmar que escenas, prefabs y `.meta` están versionados.
4. Crear:
   - `Docs/Architecture/CurrentArchitecture.md`
   - `Docs/Architecture/TargetArchitecture.md`
   - `Docs/Testing/GameplayRegressionChecklist.md`
5. Registrar valores actuales del Inspector del jugador, zombie, spawner y audio manager mediante capturas o documentación.
6. Ejecutar el checklist completo en Play Mode.

### No modificar

- Scripts de gameplay.
- Prefabs.
- Escenas.
- Animator Controllers.
- Audio Mixer.

### Commit

`docs: capture gameplay baseline and modular refactor plan`

### Criterio de salida

El proyecto compila en Unity y existe una lista reproducible de comportamiento esperado.

---

## Fase 1 — Estabilización del spawner

### Archivos

- `Enemy/ZombieSpawnerManager.cs`

### Modificaciones

1. Reescribir `ProcessSpawnQueue()` con llaves explícitas.
2. Mantener exactamente:
   - cola pendiente;
   - pausa cuando `spawningEnabled` es falso;
   - límite de zombies vivos;
   - espera entre spawns;
   - respawn retrasado.
3. No separar todavía progresión ni victoria.
4. No cambiar tiempos serializados.

### Riesgo

Bajo, pero debe verificarse el comportamiento al detener y reanudar el spawner con solicitudes pendientes.

### Pruebas

- `spawnOnStart = true`.
- `spawnOnStart = false`.
- Activación mediante trigger.
- Detención mediante trigger.
- Muerte y respawn.
- No superar `maxAliveZombies`.
- Conservar solicitudes pendientes mientras el spawner está detenido.

### Commit

`fix(spawning): clarify and stabilize spawn queue processing`

---

## Fase 2 — Modelo interno de salud del jugador

### Objetivo

Extraer la regla determinista de salud sin tocar prefabs ni mover campos serializados.

### Archivos actuales

- `Character/PlayerMovementStateMachine.cs`
- `Combat/IDamageable.cs`

### Archivos nuevos

- `Player/Health/PlayerHealthModel.cs`

### Diseño

`PlayerHealthModel` será una clase C# sin `MonoBehaviour` que manejará:

- vida máxima;
- vida actual;
- normalización del daño;
- límite entre cero y máximo;
- detección de muerte;
- evento o resultado de cambio.

`PlayerMovementStateMachine` conservará inicialmente:

- `[SerializeField] maxHealth`;
- `CurrentHealth`;
- `MaxHealth`;
- `TakeDamage(float)`;
- `IsDead`;
- animación;
- audio;
- transición de derrota.

Los métodos públicos actuarán como fachada y delegarán en el modelo.

### Motivo

Mover `maxHealth` inmediatamente a otro componente podría perder el valor guardado en el prefab. Esta fase crea el límite lógico sin migrar serialización.

### Eventos iniciales

- `HealthChanged`
- `Died`

Durante esta fase `Died` puede ser interno al jugador; todavía no se elimina la llamada al `GameManager`.

### Pruebas

- Vida inicial.
- Daño positivo.
- Daño mayor que la vida.
- Daño repetido luego de morir.
- Daño cero.
- Daño negativo.
- Animación de muerte.
- Derrota.
- HUD.
- Recarga de escena.

### Tests Edit Mode

- construcción;
- clamp;
- muerte publicada una sola vez;
- daño después de morir.

### Commit

`refactor(player): extract deterministic health model`

---

## Fase 3 — Desacoplar derrota del jugador

### Objetivo

Eliminar la dependencia directa:

```text
Player -> GameAudioManager
Player -> GameManager
```

### Archivos

- `Character/PlayerMovementStateMachine.cs`
- `Core/GameManager.cs`
- `Audio/GameAudioManager.cs`

### Modificaciones

1. El jugador publicará `Died`.
2. `GameManager` recibirá una referencia serializada al jugador o a su componente de salud.
3. `GameManager` se suscribirá en `OnEnable` y se desuscribirá en `OnDisable`.
4. Al recibir la muerte, ejecutará `ShowDefeat()`.
5. El audio de derrota será consecuencia del cambio de estado de `GameManager`, no una llamada realizada por el jugador.
6. Añadir `GameStateChanged`.

### Compatibilidad

- No renombrar `ShowDefeat`.
- No cambiar los roots de UI.
- No cambiar `Time.timeScale`.
- No modificar el timing de muerte.

### Pruebas

- Muerte del jugador.
- Evento disparado una sola vez.
- Pantalla de derrota.
- Música de derrota.
- Cursor.
- Pausa.
- Recarga de escena.
- Deshabilitar y volver a habilitar objetos.

### Commit

`refactor(game-flow): handle player defeat through events`

---

## Fase 4 — Input unificado del jugador

### Objetivo

Tener una única lectura de teclado, mouse y gamepad.

### Archivos nuevos

- `Player/Input/PlayerInputReader.cs`

### Archivos modificados

- `Character/PlayerMovementStateMachine.cs`
- `Combat/PlayerRevolverShooter.cs`
- `Camera/ThirdPersonCameraFollow.cs`
- eventualmente `Core/GameManager.cs`

### Responsabilidad de `PlayerInputReader`

- movimiento;
- correr;
- apuntar;
- disparo;
- look;
- shoulder swap;
- eventos de cambios relevantes.

### Migración segura

1. Crear el componente.
2. Mantener temporalmente las propiedades públicas actuales de `PlayerMovementStateMachine` como wrappers:
   - `MoveInput`
   - `IsRunPressed`
   - `IsAimPressed`
   - `IsShootPressed`
   - `IsShootPressedThisFrame`
3. El arma usa directamente `PlayerInputReader`.
4. La cámara usa directamente `PlayerInputReader`.
5. Solo después se retira la lectura duplicada.

### No hacer todavía

- Migrar a Input Actions.
- Remapeo.
- Action Maps.
- Cambiar sensibilidad.
- Permitir caminar mientras se apunta.

### Pruebas

- teclado y mouse;
- gamepad;
- correr;
- apuntar;
- disparar;
- mantener botones;
- disparo por frame;
- shoulder swap;
- cámara;
- input bloqueado al morir;
- pausa.

### Commit

`refactor(player): centralize gameplay input reading`

---

## Fase 5 — Motor de movimiento del jugador

### Objetivo

Separar movimiento físico de la máquina de estados.

### Archivos nuevos

- `Player/Movement/PlayerMotor.cs`

### Archivos modificados

- `Character/PlayerMovementStateMachine.cs`
- estados Idle, Walk y Run.

### Responsabilidad de `PlayerMotor`

- `CharacterController`;
- movimiento horizontal;
- gravedad;
- dirección relativa a cámara;
- rotación;
- rotación durante apuntado.

### Estrategia de serialización

Primera etapa:

- las velocidades permanecen serializadas en `PlayerMovementStateMachine`;
- se pasan a `PlayerMotor` durante inicialización;
- el prefab no necesita cambios de valores.

Segunda etapa, en un commit posterior:

- migrar los campos al nuevo componente manualmente;
- verificar prefab;
- retirar campos antiguos.

### Estado final de la máquina

`PlayerMovementStateMachine` conserva:

- estado actual;
- transiciones;
- coordinación de input, motor y presentación;
- reglas especiales de muerte y apuntado.

### Commit

`refactor(player): extract character motor from state machine`

---

## Fase 6 — Presentación del jugador

### Vertical slices

### 6A. Animación

Nuevo archivo:

- `Player/Presentation/PlayerAnimationController.cs`

Responsabilidades:

- hashes;
- actualización de parámetros;
- trigger de muerte.

No cambiar nombres de parámetros.

Commit:

`refactor(player): isolate animator parameter updates`

### 6B. Pasos

Nuevo archivo:

- `Player/Presentation/PlayerFootstepAudio.cs`

Responsabilidades:

- intervalos;
- selección aleatoria;
- pitch;
- `AudioSource`;
- mixer group.

Migración:

- conservar fallback temporal para crear el AudioSource;
- luego agregar el componente/fuente correctamente al prefab.

Commit:

`refactor(player): extract footstep audio behavior`

---

## Fase 7 — Combate del jugador

### Archivos

- `Combat/PlayerRevolverShooter.cs`
- `Combat/BulletProjectile.cs`

### Modificaciones

1. El arma depende de `PlayerInputReader`, no de la máquina de movimiento.
2. Añadir protección explícita contra disparo estando muerto.
3. Mantener:
   - cooldown;
   - raycast central;
   - punto de mira;
   - capas;
   - creación del proyectil;
   - audio.
4. Preparar `BulletProjectile` para reiniciar:
   - `hasImpacted`;
   - owner;
   - velocidad;
   sin introducir pooling todavía.
5. Mantener `IDamageable`.

### No hacer

- munición;
- recarga;
- pooling;
- recoil;
- hit reactions;
- `DamageInfo`;
- cambio de física.

### Commit

`refactor(combat): decouple revolver firing from movement state`

---

## Fase 8 — Salud del zombie

### Archivos nuevos

- `Zombies/Health/ZombieHealthModel.cs`

### Archivos modificados

- `Enemy/ZombieStateMachine.cs`
- `Enemy/ZombieSpawnerManager.cs`

### Estrategia

Repetir el enfoque seguro del jugador:

1. Mantener `maxHealth` serializado en `ZombieStateMachine`.
2. Delegar reglas a un modelo puro.
3. Mantener temporalmente:
   - `TakeDamage`;
   - `IsDead`;
   - `ZombieDied`;
   - animación;
   - audio;
   - cambio a `DeadState`.
4. Publicar:
   - `HealthChanged`;
   - `Died`.

### Compatibilidad

`ZombieDied` puede conservarse como evento adaptador hasta que el spawner migre.

### Commit

`refactor(zombies): extract deterministic zombie health model`

---

## Fase 9 — Targeting y navegación del zombie

### Archivos nuevos

- `Zombies/Targeting/ZombieTarget.cs`
- `Zombies/Navigation/ZombieNavigation.cs`

### Modificaciones

`ZombieNavigation` manejará:

- `NavMeshAgent`;
- destination;
- stop/resume;
- stopping distance;
- rotación;
- disable al morir.

`ZombieTarget` manejará:

- referencia al target;
- distancia;
- disponibilidad.

### Inicialización

Añadir un método explícito:

```csharp
Initialize(Transform playerTarget)
```

El spawner pasará el target a cada zombie creado.

Para zombies existentes en escena:

1. referencia serializada;
2. inicialización desde un coordinador;
3. fallback global temporal con warning.

El fallback solo se elimina después de migrar la escena y prefabs.

### Commit

`refactor(zombies): isolate target and navigation responsibilities`

---

## Fase 10 — Combate del zombie

### Archivo nuevo

- `Zombies/Combat/ZombieAttackController.cs`

### Responsabilidades

- rango;
- cooldown;
- daño;
- resolución de `IDamageable`;
- solicitud de animación y audio.

### Regla crítica

El daño continuará ocurriendo al comenzar el ataque. La migración a Animation Event será una mejora de gameplay posterior y no parte del refactor.

### Commit

`refactor(zombies): extract attack timing and damage logic`

---

## Fase 11 — Estados del zombie

### Objetivo

Reducir la API pública de `ZombieStateMachine`.

### Posible contrato

- `IZombieStateContext`

Solo se introduce si reduce acoplamiento de forma clara. No se creará un framework genérico compartido con el jugador.

### Estado final

`ZombieStateMachine`:

- posee el estado actual;
- coordina componentes;
- expone un contexto mínimo;
- no contiene salud, navegación, combate o audio detallado.

### Commit

`refactor(zombies): narrow state machine context`

---

## Fase 12 — Spawning y progresión

### Archivos nuevos

- `Spawning/Progression/ZombieKillProgress.cs` o componente equivalente.

### Archivos modificados

- `Enemy/ZombieSpawnerManager.cs`
- `Core/GameManager.cs`
- `UI/InGameHudController.cs`

### Separación

`ZombieSpawnerManager` conserva:

- prefab;
- spawn points;
- cola;
- límites;
- registro de vivos;
- respawn.

La progresión conserva:

- total de muertos;
- umbral de victoria;
- `TotalKilledChanged`;
- `VictoryThresholdReached`.

### Desacoplamiento

Eliminar del spawner:

- búsqueda de `GameAudioManager`;
- búsqueda de `GameManager`;
- reproducción directa de música;
- apertura directa de victoria.

`GameManager` escucha `VictoryThresholdReached`.

### Commit

`refactor(spawning): separate kill progression from spawning`

---

## Fase 13 — GameFlow

### Archivos

- `Core/GameManager.cs`

### Responsabilidades finales

- estado de juego;
- pausa;
- victoria;
- derrota;
- cursor;
- time scale;
- activación de pantallas;
- transición a menú;
- `GameStateChanged`.

### Audio

El audio reaccionará a `GameStateChanged` o será comandado por el coordinador. Se elegirá una sola estrategia, no ambas.

### No renombrar inicialmente

`GameManager` no se renombra todavía para evitar riesgo en escenas y UnityEvents.

### Commit

`refactor(game-flow): centralize outcome and pause coordination`

---

## Fase 14 — HUD reactivo

### Archivos

- `UI/InGameHudController.cs`

### Modificaciones

Reemplazar polling de cada frame por suscripciones:

- salud;
- kills;
- apuntado;
- muerte;
- game state.

Mantener una actualización inicial en `OnEnable`.

### Ciclo de vida

- suscribir en `OnEnable`;
- desuscribir en `OnDisable`;
- evitar suscripciones duplicadas.

### Commit

`refactor(ui): update gameplay HUD from module events`

---

## Fase 15 — Audio modular

### Mantener una fachada

`GameAudioManager` puede continuar como punto público y singleton durante la migración.

### Componentes internos posibles

- `AudioMixerController`
- `GameplaySnapshotController`
- `AdaptiveMusicController`
- `OutcomeMusicController`
- `AudioPauseController`

### Orden

1. Agregar limpieza segura de `Instance` en `OnDestroy`.
2. Corregir el ciclo de vida de voz.
3. Extraer mixer.
4. Extraer snapshots.
5. Extraer música.
6. Revisar pausa de fuentes.

### Riesgos

- nombres de Mixer Groups;
- parámetros expuestos;
- snapshots;
- objetos persistentes entre escenas;
- coroutines;
- `Time.timeScale = 0`.

### Commit por responsabilidad

No combinar toda la separación en un único commit.

---

## Fase 16 — Cámara

### Archivos

- `Camera/ThirdPersonCameraFollow.cs`

### Modificaciones

1. Consumir `PlayerInputReader`.
2. Mantener perfiles y sensibilidades.
3. Reemplazar `SphereCastAll` por una solución sin allocations solamente después de pruebas equivalentes.
4. Mantener filtrado del target y shoulder swap.

### Commit

`refactor(camera): consume shared player input state`

Luego:

`perf(camera): avoid collision query allocations`

---

## Fase 17 — Organización física, namespaces y asmdefs

Esta fase ocurre después de estabilizar todos los límites.

### Orden obligatorio

1. Mover scripts con sus `.meta`.
2. Verificar escenas y prefabs.
3. Añadir namespaces.
4. Compilar.
5. Añadir asmdefs.
6. Corregir únicamente dependencias permitidas.

### Asmdefs propuestos

- `WesternShooter.Shared`
- `WesternShooter.Combat`
- `WesternShooter.Player`
- `WesternShooter.Zombies`
- `WesternShooter.Spawning`
- `WesternShooter.Audio`
- `WesternShooter.GameFlow`
- `WesternShooter.Camera`
- `WesternShooter.UI`

### Tests

- un asmdef de tests Edit Mode;
- Play Mode solo para integración Unity necesaria.

### Commit

Separar movimientos, namespaces y asmdefs en commits diferentes.

---

## Fase 18 — Tests y endurecimiento

### Edit Mode

- salud del jugador;
- salud del zombie;
- reglas de daño;
- cooldowns;
- progresión de kills;
- condición de victoria;
- política de población;
- transiciones puras, si se extraen.

### Play Mode

- spawn;
- muerte del zombie;
- suscripciones;
- derrota;
- victoria;
- pausa;
- UI;
- audio;
- recarga de escena.

### No hacer

No simular excesivamente `Animator`, `NavMeshAgent` o físicas con mocks complejos.

---

## Fase 19 — Mejoras posteriores, no refactor

Estas tareas deben mantenerse fuera de los commits arquitectónicos:

- daño del zombie mediante Animation Event;
- caminar mientras se apunta;
- recarga y munición;
- pooling;
- hit reactions;
- remapeo de Input Actions;
- guardado;
- mejoras de IA;
- nueva UI;
- cambios de balance.

---

# 6. Eventos públicos propuestos

## Player

```text
HealthChanged(current, maximum)
Died
AimChanged(isAiming)
```

## Zombies

```text
HealthChanged(current, maximum)
Died(zombie)
```

## Spawning/Progression

```text
AliveCountChanged(current, maximum)
TotalKilledChanged(total)
VictoryThresholdReached(total)
```

## GameFlow

```text
GameStateChanged(previous, current)
```

## Eventos que no se deben crear

- `MoveRequested`
- `DamageRequested`
- `FireRequested`
- `StartSpawnRequested`

Esas acciones deben seguir siendo métodos directos.

---

# 7. Política de serialización

1. No mover un campo `[SerializeField]` entre componentes en el mismo commit que se extrae la lógica.
2. Primero crear una fachada o modelo interno.
3. Después agregar el nuevo componente.
4. Copiar valores explícitamente en el prefab.
5. Verificar prefab overrides.
6. Recién entonces retirar el campo antiguo.
7. Usar `FormerlySerializedAs` para renombres dentro del mismo objeto.
8. Recordar que `FormerlySerializedAs` no migra datos entre componentes diferentes.
9. Mover siempre los `.meta` junto con scripts y carpetas.
10. No renombrar métodos usados por botones, Animation Events o UnityEvents sin revisar sus referencias.

---

# 8. Checklist obligatorio por commit

## Compilación

- Unity termina de recompilar.
- Console sin errores nuevos.
- Warnings nuevos revisados.
- No hay Missing Script.

## Player

- movimiento;
- rotación;
- correr;
- apuntar;
- disparar;
- gravedad;
- pasos;
- daño;
- muerte;
- input deshabilitado;
- derrota.

## Zombies

- detección del jugador;
- chase;
- ataque;
- cooldown;
- daño;
- pasos;
- muerte;
- animación;
- audio;
- NavMesh.

## Spawner

- inicio;
- trigger;
- stop;
- cola;
- límite;
- respawn;
- kills;
- victoria.

## GameFlow/UI

- pausa;
- opciones;
- reanudar;
- cursor;
- victory;
- defeat;
- volver al menú;
- HUD.

## Audio

- peaceful;
- wanted;
- voz;
- pasos;
- disparo;
- ataque;
- muerte;
- victoria;
- derrota;
- pausa;
- cambio de escena.

---

# 9. Orden de commits recomendado

1. `docs: capture gameplay baseline and modular refactor plan`
2. `fix(spawning): clarify and stabilize spawn queue processing`
3. `refactor(player): extract deterministic health model`
4. `refactor(game-flow): handle player defeat through events`
5. `refactor(player): centralize gameplay input reading`
6. `refactor(player): extract character motor from state machine`
7. `refactor(player): isolate animator parameter updates`
8. `refactor(player): extract footstep audio behavior`
9. `refactor(combat): decouple revolver firing from movement state`
10. `refactor(zombies): extract deterministic zombie health model`
11. `refactor(zombies): isolate target and navigation responsibilities`
12. `refactor(zombies): extract attack timing and damage logic`
13. `refactor(zombies): narrow state machine context`
14. `refactor(spawning): separate kill progression from spawning`
15. `refactor(game-flow): centralize outcome and pause coordination`
16. `refactor(ui): update gameplay HUD from module events`
17. commits pequeños para audio
18. `refactor(camera): consume shared player input state`
19. `perf(camera): avoid collision query allocations`
20. movimientos de archivos
21. namespaces
22. asmdefs
23. tests adicionales

---

# 10. Primer trabajo a ejecutar

El primer cambio de código debe limitarse a:

```text
Enemy/ZombieSpawnerManager.cs
```

Objetivo:

- corregir la estructura de `ProcessSpawnQueue`;
- agregar llaves claras;
- preservar semántica;
- no tocar progresión, audio, victoria ni serialización.

Después de verificarlo en Play Mode, el segundo trabajo será crear el modelo interno de salud del jugador.

No se debe pedir a Copilot que ejecute todas las fases automáticamente. Cada fase se revisa, compila y prueba antes de aprobar la siguiente.
