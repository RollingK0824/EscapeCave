# CLAUDE.md

이 파일은 이 저장소에서 작업할 때 Claude Code(claude.ai/code)에게 제공되는 가이드입니다.

## 프로젝트 개요

EscapeCave(가제)는 2D 유니티 게임(Unity 6 / 6000.3.18f1, URP)입니다. 청크 단위로 만들어지는 무한 스크롤 동굴, 에코로케이션(반향위치) 기반 시야, 비헤이비어 트리로 동작하는 순찰/어그로 몬스터, 그래플 훅 기반 플레이어 이동을 특징으로 하는 절차적 생성 동굴 탈출 게임입니다.

CLI 빌드/테스트 워크플로우는 없습니다 — 유니티 에디터 프로젝트입니다. 실행/빌드/테스트하려면 프로젝트와 동일한 버전(`6000.3.18f1`, `ProjectSettings/ProjectVersion.txt` 참고)의 유니티 에디터로 열어야 합니다. 자동화된 테스트 스위트는 없으며(`com.unity.test-framework` 패키지는 설치되어 있지만 사용하지 않음), 아래 전용 테스트 씬을 통해 수동으로 테스트합니다.

## 디렉토리 구조

외부 에셋(`Assets/ThirdParty/`)과 혼선을 막기 위해 팀이 제작하는 모든 코드/에셋은 오직 `Assets/Project/` 아래에서만 관리합니다:

- `Scripts/` — C# 코드 (아래 아키텍처 참고)
- `Prefabs/` — 완성된 재사용 프리팹. 씬 인스턴스를 직접 수정하기보다 프리팹을 수정하는 것을 우선한다
- `Scenes/` — 기능/담당자별로 분리된 씬 (아래 참고)
- `Data/` — ScriptableObject 에셋 (맵 규칙, 타일 데이터, 스테이지 매니페스트, 아이템 데이터)
- `Animations/`, `Audio/`, `Shaders/`, `Visuals/` — 아트/오디오/셰이더 에셋

주요 씬: `ProtoType.unity`(메인 통합 씬), `EchoSystemTestScene.unity`, `PlayerControlTest.unity`, `RandomMapGeneratorTest.unity`, `Enemy.unity`, `Title Screen.unity`, `UI.unity`. 특정 시스템을 단독으로 테스트할 때는 전체 프로토타입 씬 대신 해당 전용 테스트 씬을 사용한다.

## 아키텍처

### 절차적 맵 생성 (`Scripts/Game/MapGenerator/`)
`MapGenerator`는 하나의 전역 `Tilemap`에서 항상 정확히 3개의 청크를 유지하며, 순환하는 `_chunkOffsets`/`_currentChunkIdx`로 인덱싱합니다. 플레이어가 현재 청크 오프셋 기준 `1.5배` 청크 너비를 넘어서면 `ShiftChunks()`가 가장 오래된(가장 왼쪽) 청크를 재활용합니다: 해당 청크의 타일을 지우고, 스폰된 오브젝트를 `PoolManager`로 반납한 뒤, *이전* 청크의 출구 지점(`_lastExitY`/`_lastPlatformLocal`)을 진입점으로 삼아 그 자리에 새 청크를 생성합니다. 이 방식으로 청크 경계를 넘어도 지형이 끊기지 않고 이어집니다.

청크 내용 자체는 `BaseMapRuleSO`(추상 ScriptableObject)가 생성하며, 테마별 서브클래스인 `CaveRuleSO` / `LakeRuleSO`가 `CarveTerrain()`과 `PlacePlatforms()`를 구현합니다. 베이스 클래스의 `GenerateChunk()`는 모든 규칙이 따르는 고정 파이프라인입니다: `InitializeMap → CarveTerrain → ForceTransitionTunnel → PlacePlatforms → RenderToTilemap → SpawnObjects`. 청크마다 시드가 부여되는 `System.Random`(`masterSeed + chunkGenerationCount`)이 결정적 생성을 담당하며, `MapGenerator`의 `stageRules[]`가 청크마다 선택되는 테마 풀입니다. 오브젝트 스폰(`SpawnObjects`/`TrySpawnObject`)은 생성된 타일 그리드를 순회하며 플랫폼/바닥/천장 표면을 찾고 `SpawnRule` 확률을 굴려, `minSpawnGapX` 간격을 지키면서 `PoolManager`에서 인스턴스를 꺼내 `MapSpawnedObject.poolKey`로 태그해 나중에 재활용할 수 있게 합니다.
플레이어 거리 기반 컬링(`UpdateObjectCulling`)은 청크 이동과 무관하게, `cullingDistance`를 벗어난 스폰 오브젝트를 독립적으로 비활성화합니다.

### 몬스터 AI (`Scripts/Game/Monster/`)
몬스터는 직접 만든 FSM이 아니라 유니티의 **Behavior** 패키지(`com.unity.behavior`, 비주얼 비헤이비어 트리 그래프 + `BehaviorGraphAgent`)를 사용합니다. `MonsterController`는 비헤이비어 그래프가 호출하는 런타임용 API(이동, 순찰, 감지, 피해, `MonsterState` enum을 통한 상태 전환)일 뿐, 몬스터 로직 자체를 결정하지 않습니다. 실제 의사결정 로직(순찰, 추격, 공격, 스턴, 어그로 리셋, 돌진 등)은 이 폴더 안의 개별 `Action`/`Condition` 노드 클래스(`SpawnAction`, `MoveToTargetAction`, `IsInAttackRangeCondition`, `StunAction`, `KnockbackAction`, `DieAction` 등)에 있으며, 각각 `[Serializable, GeneratePropertyBag]` partial 클래스에 `[NodeDescription(...)]` 어트리뷰트가 붙어 있고, `BlackboardVariable<T>` 필드(`Monster`, `PlayerTransform`, `SoundTrigger`, `VibTrigger`, `IsDetected`, `IsHit` 등 — `MonsterController.Awake()`에서 한 번 설정)를 통해 공유 상태를 참조합니다. 몬스터별 세부 수치(속도, 범위, 지속시간, 자유비행 여부 등)는 `MonsterData`에 있고 `MonsterController.Data`를 통해 읽습니다. 새로운 몬스터 행동을 추가할 때는 `MonsterController`에 분기 로직을 추가하지 말고, 여기에 새 Action/Condition 노드를 만들어 에디터에서 몬스터의 비헤이비어 그래프 에셋에 연결한다.

### 에코로케이션 시야 (`Scripts/Game/Echo/`, `Managers/EchoManager.cs`)
`EchoManager`(싱글턴)는 에코 핑을 발생시키는 진입점(`TriggerSound`)입니다: `PoolManager`에서 웨이브 오브젝트를 꺼내고, 그것과 자식들을 전용 마스크 레이어(`_echoWaveMaskLayerName`, 기본값 `"EchoWaveMask"`)로 강제 전환한 뒤, `RenderTexture`를 포스트 프로세스 머티리얼(`_MaskTex`)에 넘겨 웨이브가 지나간 부분만 세계를 드러냅니다. `FullscreenEcholocationController` / `RenderTargetEcholocationController`와 `EchoWaveObject`가 렌더 텍스처/셰이더 쪽(`Shaders/FullScreenEcholocationRT.shader`)을 담당합니다. 소리·진동 기반 몬스터 감지(`SoundDetectAction`, `VibrationDetectAction`, `ISoundTrigger`, `IVibrationTrigger`, `IEchoable`)도 같은 시스템에 연결되어 있어, 몬스터가 에코 핑과 플레이어 소음에 반응할 수 있습니다.

### 공용 인프라 (`Scripts/Core/`, `Scripts/Managers/`)
- `SingletonBase<T>` — `FindFirstObjectByType` 지연 폴백과 `DontDestroyOnLoad`를 갖춘 제네릭 `MonoBehaviour` 싱글턴. `PoolManager`, `EchoManager` 등 다른 매니저들이 이를 상속한다.
- `PoolManager` — 동적 오브젝트(몬스터, 맵 오브젝트, 에코 웨이브)를 생성/파괴하는 유일하게 허용된 방법입니다. 풀은 `prefab.GetInstanceID()`로 키가 부여되며, 꺼낼 때는 `Pop(prefab, pos, rot)`, 반납할 때는 `Push(go, prefabOrPoolKey)`를 사용합니다. 풀링 대상 프리팹 타입을 `Instantiate`/`Destroy`로 직접 다루지 말 것 — 스폰된 인스턴스는 반드시 `Push`를 통해 반납해야 합니다(맵 생성기는 이를 위해 `MapSpawnedObject.poolKey`로 태그를 남깁니다).

### 플레이어 (`Scripts/Game/Player/`)
하나의 거대한 컨트롤러 대신 관심사별로 분리되어 있습니다: `PlayerMovement`, `PlayerJump`, `PlayerGrappleHook`(혀/훅 기반 이동), `PlayerTongueAttack`, `PlayerSoundEmitter`(`EchoManager`/감지 시스템에 소음을 전달)가 있고, 이들을 `PlayerController`가 조율합니다. 능력 인터페이스(`IDamageable`, `IGrabbable`, `IHookable`)를 통해 플레이어와 그것이 때리고/잡고/걸 수 있는 대상 사이를 분리합니다.

## 코딩 컨벤션

팀 자체 `README.md`에서 가져온 규칙입니다 — `Assets/Project/Scripts`의 C# 코드를 작성/수정할 때 따릅니다:

- Class/Method/Enum: `PascalCase`. Public 필드(인스펙터 노출용): `PascalCase`. Private/protected 필드: `_camelCase`(선행 언더바 필수). 지역 변수/매개변수: `camelCase`. 상수: `UPPER_SNAKE_CASE`.
- Bool 이름은 `is`/`has`/`can` 접두사를 사용한다 (`isDead`, `hasKey`, `canJump`).
- 중괄호 `{ }`는 항상 줄바꿈하여 사용하고, 한 줄짜리 본문이라도 절대 생략하지 않는다.
- 스크립트 파일명 변경은 `.meta` 파일/GUID가 깨지지 않도록 반드시 유니티 에디터 내부에서 수행한다(파일시스템에서 직접 변경 금지).

### 커밋 컨벤션
형식: `태그: 요약`, 필요 시 빈 줄 뒤에 `-` 로 상세 내용을 덧붙인다. 태그(소문자, 콜론 뒤 한 칸): `feat`(새 기능/스크립트/에셋), `fix`(버그/에러/씬·프리팹 깨짐), `refactor`(구조/변수명 개선, 동작 변화 없음), `chore`(코드 외적 작업: 폴더, 패키지, 문서). 제목은 "~했음" 같은 과거형 서술이 아니라 "구현", "수정", "제거"처럼 행동을 명확히 종결하는 형태로 작성한다.

### 협업 규칙
- `main` 브랜치에 직접 push 금지. `feature/(기능이름)` 브랜치에서 작업하고 PR 리뷰를 거쳐 병합한다.
- 매 작업 시작 전 `main`을 pull/fetch하여 최신 상태로 동기화한다.
- 가급적 한 씬에는 한 명씩만 작업하고, 공유 씬을 직접 수정하기보다 프리팹 단위로 협업한다.
