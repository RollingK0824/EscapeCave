# 프로젝트 이름 : EscapeCave(가제)

## 개발 환경 및 개발 도구
- **Engine** : Unity6 (6000.3.18f1)
- **Render Pipeline** : URP (Universal Render Pipeline)
- **IDE** : Visual Studio 2026 Community
- **Version Control** : Github ( Github Desktop )
- **Git 확장** : Git LFS, Unity SmartMerge 적용 완료

## 📁 디렉토리 구조 규칙
외부 에셋과의 혼선을 줄이고 충돌을 방지하기 위해 우리 팀이 제작하는 모든 리소스와 코드는 `Project` 폴더 내부에서만 관리합니다.

- `Assets/Project/` : 메인 작업 공간
  - `Animations/` : Animation과 Animator
  - `Audio/` : Bgm, SFX등
  - `Prefabs/` : 기능별 완성형 프리팹 (가급적 씬 직접 수정 대신 프리팹 수정)
  - `Resources/` : 런타임 동적 로드가 꼭 필요한 텍스처(Sprites), 효과음(SFX) 등
  - `Scenes/` : 담당자별/기능별 분리된 씬 공간
  - `Scripts/` : C# 스크립트 코드
  - `Shaders/` : 커스텀 셰이더 및 셰이더 그래프
  - `Visuals/` : 일반적인 Texture, Sprite, Material등 보관
- `Assets/ThirdParty/` : 에셋스토어 등 외부 다운로드 플러그인 보관

---

## 📋 코딩 컨벤션 (Coding Convention)
유니티 공식 C# 스타일을 기반으로 한 우리 팀의 최소한의 규칙입니다.

### 1. 명명 규칙
- **Class / Method / Enum**: `PascalCase` (앞 글자 대문자)
  - `public class Player : MonoBehaviour`
  - `public void MoveToTarget()`
- **Public 변수**: `PascalCase` (인스펙터 노출용)
  - `public float MoveSpeed;`
- **Private / Protected 변수**: `_camelCase` (언더바 `_` 접두사 필수!)
  - `private int _currentHp;`
- **지역 변수 / 매개 변수**: `camelCase` (첫 글자 소문자)
  - `float distance = Vector3.Distance(...);`
- **상수(const)**: `UPPER_SNAKE_CASE` (대문자와 언더바)
  - `private const int MAX_WAVE_COUNT = 5;`

### 2. Bool 변수 규칙
- 상태를 직관적으로 알 수 있도록 `is`, `has`, `can` 등의 접두사를 사용합니다.
  - 예: `isDead`, `hasKey`, `canJump`

### 3. 코드 스타일
- 중괄호(`{ }`)는 생략하지 않고 항상 줄바꿈하여 가독성을 높입니다.
- 스크립트 파일명을 바꿀 때는 **반드시 유니티 에디터 내부**에서 변경합니다. (메타 파일 깨짐 방지)

## 🚀 커밋 컨벤션 (Commit Convention)
우리 팀은 GitHub Desktop을 사용하여 커밋을 기록하며, 커밋 메시지의 일관성을 위해 아래 규칙을 준수합니다.

### 1. 커밋 메시지 구조
기본적으로 [태그] 제목 형태로 작성하며, 필요한 경우 본문을 추가합니다.

```
태그: 요약문
- 상세 작업 내용 1 (선택 사항)
- 상세 작업 내용 2 (선택 사항)
```
### 2. 커밋 태그 종류
모든 태그는 소문자로 작성하고 콜론(:) 뒤에 한 칸을 띕니다.

- `feat` : 새로운 기능 추가, 새로운 스크립트/에셋 생성
  
  - 예시: feat: 플레이어 이동 및 점프 기능 구현

- `fix` : 버그, 에러, 씬/프리팹 깨짐 현상 수정
  
  - 예시: fix: 셰이더 Y축 뒤집힘 및 암전 오류 수정

- `refactor` : 기능 변화 없이 코드 구조 개선, 변수명 변경, 구조 최적화
  
  - 예시: refactor: 웨이브 매니저 루프 구조 최적화

- `chore` : 코드 외적인 작업 (폴더 구조 생성, 패키지 설치, .gitignore/README 수정)
  
  - 예시: chore: 프로젝트 README.md 코딩 컨벤션 추가

### 3. 작성 규칙 (Rules)
`명령문/현재형 요약` : 제목은 "~했음", "~수정함" 보다는 행동을 명확히 종결하는 형태로 작성합니다. (예: 구현, 수정, 제거, 추가)

`제목과 본문 분리` : 상세 설명이 필요할 경우, 제목을 쓰고 한 줄을 비운 뒤 대시(-)를 활용해 본문을 적습니다.

## ⚠️ 협업 골든 룰 (이것만은 꼭 지키자!)
- Main 브랜치 직접 Push 절대 금지 : 모든 작업은 각자 본인의 feature/`(기능이름)` 브랜치를 파서 진행하고, 기능이 완성되면 Pull Request(PR)를 날려 팀원들의 확인을 받은 후 Main에 합칩니다.

- 작업 시작 전 Pull 필수 : 컴퓨터를 켜면 무조건 Main 브랜치에서 Fetch 및 Pull을 받아 최신 상태로 동기화한 뒤 내 브랜치를 생성하거나 이동합니다.

- 1인 1씬(Scene) 원칙 : 가급적 레벨 디자인이나 메인 화면 등 큰 작업이 아니라면, 오브젝트를 프리팹(Prefab)으로 만들어 각자의 브랜치에서 프리팹 단위로 협업합니다.
