# Valor Chronicle 신규 캐릭터 구현 가이드

> 상태: 세레나 구현 구조 반영  
> 기준: 2-1~2-6B와 세레나 4-1~4-6B 구현 및 4-6C 정적 검증 기준  
> 범위: 캐릭터 전용 코드·콘텐츠·테스트를 추가하는 절차. 공통 전투 공식을 새로 만들거나 복제하는 절차가 아니다.

## 문서 사용 원칙

이 문서에서 **현재 구현**은 저장소에서 확인한 동작, **구현 절차**는 신규 캐릭터가 따라야 할 규칙, **미구현/확인 필요**는 아직 코드로 검증되지 않은 설계를 뜻한다. 설계 문서와 코드가 다르면 현재 런타임 동작은 코드를 기준으로 판단하고, 차이는 임의로 숨기지 않는다.

주요 설계 근거는 `Docs/Design/Valor Chronicle 공통 규칙 확정안.docx`, `Docs/Design/Valor Chronicle 기능 구현 구조 및 데이터 관리 가이드.docx`, `Docs/Design/캐릭터 설계.docx`, `Docs/Design/Character/마레아 블루팽 캐릭터 템플릿.docx`, `Docs/Design/Character/세레나 코랄스피어 캐릭터 템플릿.docx`이다.

공통 시스템은 캐릭터별로 복제하지 않는다. 특히 `DamageCalculator`, `DamageContextFactory`, `MatchEventCombatActionFactory`, `CombatActionExecutionSession`, `CombatTriggerResolver`, `CombatResourceRequirementMerger`, `BattleSceneCombatComposition`, `BattleHudController`에 캐릭터 ID 분기를 추가하지 않는 것이 기본 원칙이다.

---

## 제1장. 신규 캐릭터 구현 전체 흐름

다음 순서로 작게 구현하고 각 단계에서 해당 계층의 테스트를 통과시킨다.

| 순서 | 목적 | 생성·수정 대상 | 연결 시스템 | 완료 확인 |
|---|---|---|---|---|
| 1 | ID와 설계를 확정한다 | 캐릭터 설계 문서 | 저장·콘텐츠의 안정 ID 규칙 | 소문자·숫자·단일 밑줄 형식이며 모든 계층에서 같은 ID를 사용한다 |
| 2 | 전투 수치의 권위 있는 원본을 만든다 | 캐릭터별 `CombatConfigDefinition` 파생 클래스와 에셋 | `CharacterDefinition.CombatConfig` | `TryValidate`가 모든 계수·기간·상한을 검증한다 |
| 3 | 기본 콘텐츠를 만든다 | `CharacterDefinition`, `SkillDefinition` 에셋 | `DefinitionDatabase` | ID·스킬 참조·CombatConfig 참조가 유효하다 |
| 4 | 영구 성장 보정을 구현한다 | `ICharacterBuildModifier` 구현 | `CharacterBuildResolver` | 레벨/각성별 HP·ATK 결과가 정확하다 |
| 5 | 각성별 전투 규칙을 한곳에서 해석한다 | 필요 시 캐릭터별 Rule Resolver | Match/Active/Passive Provider | 동일 각성 효과가 여러 계층에서 중복 적용되지 않는다 |
| 6 | 매칭 행동을 구현한다 | `IMatchEventActionProvider` 구현 | `MatchEventCombatActionFactory` | 3/4/5매칭의 액션, ID, 계보, 자원 시점이 정확하다 |
| 7 | 액티브 행동을 구현한다 | `IActiveAbilityActionProvider` 구현 | Active 실행기와 `BattleFlowCoordinator` | 시작 사용 가능, 쿨타임, 효과 기간, 액션 계보가 정확하다 |
| 8 | 패시브/이벤트 반응을 구현한다 | 상시 보정 또는 `ICombatTriggerRule` 구현 | `CombatTriggerResolver` | 조건, 횟수, 소유자, 후속 연쇄 제한이 정확하다 |
| 9 | Provider와 전투별 고유 상태를 묶는다 | `ICharacterCombatProviderRegistrar` 구현 | Provider Registry | Match/Active 등록, Trigger·Resource·Status 기여 및 상태 공유가 정확하다 |
| 10 | 코드 등록을 한곳에 추가한다 | `CharacterRuntimeRegistrationCatalog.CreateDefault()` | Build/Combat/Lookup 세 경로 | 동일 ID로 세 구성 요소가 생성된다 |
| 11 | 표시·획득 콘텐츠를 연결한다 | Presentation, Localization, Database, Gacha/Exchange 에셋 | HUD, 도감/목록, 획득 경로 | 캐릭터별 UI 하드코딩 없이 표시된다 |
| 12 | 자동·수동 검증을 완료한다 | 캐릭터 테스트와 콘텐츠 검증 | EditMode/PlayMode/실제 씬 | 개별·공통 회귀·씬 검증이 모두 완료된다 |

신규 캐릭터 폴더는 마레아의 `Assets/Game/Characters/Marea/`를 참고하되, 파일을 복사한 뒤 이름만 바꾸는 방식으로 공통 계산을 중복 구현하지 않는다. 캐릭터 폴더에는 고유 Config, 규칙 해석, Provider, Trigger, Registrar, Lookup Provider처럼 정말 캐릭터 고유인 코드만 둔다.

## 제2장. 캐릭터 데이터 및 CombatConfig

### 역할과 권위

- `Assets/Game/Data/Definitions/CharacterDefinition.cs`
  - 안정적인 `CharacterId`, 표시명 키, 원소, 레벨 1/100 HP·ATK, 스킬 ID 목록, `CombatConfigDefinition` 참조를 가진다.
  - 기본 능력치와 콘텐츠 관계의 원본이다. 전투 중 현재 HP나 임시 버프를 저장하지 않는다.
- `Assets/Game/Data/Definitions/CombatConfigDefinition.cs`
  - 캐릭터별 전투 설정의 공통 기반이다. 파생 Config가 `TryValidate(out string error)`로 자체 무결성을 검사한다.
- `Assets/Game/Data/Definitions/SkillDefinition.cs`
  - 스킬 ID, 표시명/설명 키, 아이콘, `SkillKind` 등 표시용 정의를 가진다.
  - 실제 피해 계산이나 런타임 쿨타임 상태를 보유하지 않는다.
- `Assets/Game/Battle/Combat/State/CharacterBattleState.cs`
  - 전투마다 생성되는 런타임 상태다. 정의 에셋과 저장 데이터에 되돌려 쓰지 않는다.

캐릭터별 CombatConfig에는 매칭 계수, 액티브 수치·기간·쿨타임, 패시브 수치, 고유 자원 관련 값 등 **조정 가능한 캐릭터 고유 전투 수치**를 둔다. 마레아 예시는 `Assets/Game/Characters/Marea/MareaBluefangCombatConfig.cs`이며, `MareaBluefangCombatRuleResolver`가 각성 단계에 맞는 실제 전투 값을 해석한다.

### 구현 절차

1. 저장과 콘텐츠 참조에 사용할 `CharacterId`를 먼저 확정한다. 한번 저장 데이터에 사용한 ID는 마이그레이션 없이 바꾸지 않는다.
2. `CombatConfigDefinition` 파생 타입을 만들고 모든 직렬화 필드를 `TryValidate`에서 검사한다. 0 이하가 불가능한 계수·기간·쿨타임, 범위를 벗어난 확률, 공용 자원 상한 불일치를 명시적으로 거부한다.
3. `CharacterDefinition`에 기본 능력치, 원소, 스킬 ID, Config를 연결한다.
4. 각 `SkillDefinition`을 만들고 Database에 포함한다.
5. 수치가 설계 문서와 Config 중 어디에 중복 기재되더라도 런타임 입력의 권위는 Config 에셋이다. 문서는 의도와 검수 기준이며 런타임 원본이 아니다.

### 마레아에서 확인된 규칙

`MareaBluefangCombatConfig`의 물 원소 상한은 `WaterElementResource.MaxAmount`와 같아야 유효하다. 현재 공용 물 원소 상한은 5다. 신규 캐릭터가 같은 자원을 쓴다고 별도 상한을 만들지 않는다.

## 제3장. Build Modifier와 각성 처리

`Assets/Game/Characters/Build/ICharacterBuildModifier.cs`는 캐릭터 ID와 각성 단계에 따른 영구 능력치 배율을 제공한다. `Assets/Game/Characters/Build/CharacterBuildResolver.cs`는 다음을 담당한다.

- 레벨 1~100 기본 능력치를 `CharacterStatCalculator`로 직접 선형 보간한다.
- 최종 양수 능력치를 `MidpointRounding.AwayFromZero`로 반올림한다.
- Build Modifier의 HP·ATK 배율을 적용한다.
- 유물의 최대 HP 증가는 Build 단계에 적용한다.
- 공격 피해에 적용되는 유물 보정은 전투 피해 수집 단계에 맡긴다.

마레아의 `MareaBluefangBuildModifier`는 1각성 ATK, 2각성 HP 같은 영구 능력치만 처리한다. 패시브 강화, 매칭 계수 변경, 자원당 피해 변경, 잔류 쇄도는 `MareaBluefangCombatRuleResolver`와 전투 Provider가 처리한다.

각성 구현 위치는 다음 기준으로 선택한다.

| 효과 종류 | 구현 위치 |
|---|---|
| 전투 시작 전 확정되는 영구 HP/ATK 배율 | `ICharacterBuildModifier` |
| 매칭/액티브/패시브 계수 또는 조건 변화 | 캐릭터별 Combat Rule Resolver와 Provider |
| 전투 중 획득·소비되는 임시 상태 | `CharacterBattleState`, Effect 또는 캐릭터별 전투 상태 |
| 저장되는 소유·레벨·각성 값 | Save DTO; 파생 최종 능력치는 저장하지 않음 |

같은 각성 효과를 Build Modifier와 Provider에 동시에 적용하지 않는다. 각성 0~6 범위 검증은 기존 Resolver 계약을 유지하고, 각 각성 경계값을 캐릭터 테스트로 고정한다.

## 제4장. Match Action Provider

신규 Provider는 `Assets/Game/Battle/Combat/Integration/IMatchEventActionProvider.cs`의 `CreateRootActions(MatchEventActionContext context)`를 구현한다. Context에서 공격자, 매칭 이벤트, 최종 콤보 수, 매칭 공격 태그, 파티, 보스, 전투 공용 `CombatActionIdSequence`를 받는다.

### 표준 생성 절차

1. 지원하는 캐릭터와 3/4/5매칭 티어인지 검증한다.
2. 공격 시작 시 확정해야 하는 자원량·조건·캐릭터 고유 계수를 조회한다.
3. `context.ActionIds.Next()`로 Match `DamageAction` ID를 발급한다.
4. `DamageContextBuildRequest`에 공격자/파티/보스, 원소, `AttackType.Match`, 티어 태그, 스킬 계수, 콤보 적용 여부와 최종 콤보 수, 치명타 허용 여부, 액션 로컬 보정을 넣는다.
5. 피해 뒤 자원 획득·소비·효과 적용이 필요하면 별도 Action을 생성한다.
6. 실제 실행되어야 할 순서로 목록을 반환한다.

Match Provider가 반환한 모든 Action은 한 번의 호출에서 공용 ID Sequence를 정확히 한 번씩 사용하며, 반환 목록과 ID 발급은 연속적이어야 한다. `MatchEventCombatActionFactory`가 이를 검증한다.

### 공격 시작 시 확정과 실행 시 수집의 차이

마레아는 Provider가 호출될 때 현재 물 원소량을 읽어 5매칭 소비량과 계수, 조건부 로컬 보정을 확정한다. 그 뒤 Action 실행 중 자원량이 달라져도 이미 만든 요청의 값은 바뀌지 않는다. 반면 일반 Effect·공격 유형별 보정은 각 `DamageAction` 실행 시 `DamageContextFactory.Build`가 수집한다. 신규 스킬은 무엇을 “공격 시작 시 확정”할지 명시하고 이 두 시점을 혼동하지 않는다.

### 피해와 자원 액션 순서

- 획득형: 보통 `[DamageAction, AddResourceAction]`으로 피해 뒤 자원을 얻는다.
- 소비형: 보통 `[DamageAction, ConsumeResourceAction]`으로 공격 시작 시 확정한 양의 피해를 먼저 주고 실제 소비를 기록한다.
- `ConsumeResourceAction`은 실제 소비량이 1 이상일 때만 `ResourceConsumptionRecord`를 남긴다.

마레아 5매칭이 이 소비형 기준선이다. 3/4매칭은 피해 뒤 물 원소를 얻으며 최대 5를 넘지 않는다.

### 단일 조건부 Match와 별도 Additional의 구분

현재 마레아 4매칭은 물 원소 조건이 맞으면 계수를 합산한 **하나의 `AttackType.Match` DamageAction**이다. 설계 문서의 “추가 피해”라는 자연어가 별도 `Additional` Action을 뜻하지는 않는다. 따라서 Match 피해 증가 효과가 전체 합산 계수에 적용되고 Additional 전용 효과는 적용되지 않는다.

별도 추가 공격은 Match와 Additional을 분리한다.

- 첫 Action: `ActionOrigin.Match`, `AttackType.Match`, 자기 자신이 Root인 Match DamageAction.
- 둘째 Action: `ActionOrigin.Additional`, `AttackType.Additional`.
- Additional의 `RootActionId`와 `SourceActionId`는 모두 바로 앞의 유효한 Match DamageAction ID여야 한다.
- 공격자, 파티, 보스, 원소, 매칭 티어 태그, 콤보 수는 Factory의 검증 조건과 일치해야 한다.

이 분리로 각 공격이 실행되는 시점에 Match/Additional 전용 피해 증가를 독립적으로 수집한다. 기존 `DamageCalculator` 공식을 복제하지 않는다.

## 제5장. Active Action Provider

`Assets/Game/Battle/Combat/Integration/IActiveAbilityActionProvider.cs`의 `CreateRootActions(ActiveAbilityActionContext context)`를 구현한다. Context는 활성 바인딩, 캐릭터, 파티, 보스, 공용 Action ID Sequence를 제공한다.

### 등록과 실행

- Registrar가 Active Provider를 Registry에 등록한다.
- `CharacterCombatProviderRegistrationResult.WithActiveAbility`로 액티브 ID와 쿨타임을 Composition에 전달한다.
- 런타임 상태는 전투 시작 시 남은 쿨타임 0으로 시작하므로 액티브를 사용할 수 있다.
- 사용하면 현재 계약상 남은 쿨타임이 전체 쿨타임으로 설정되고 같은 턴 재사용이 막힌다. 턴 시작 처리에서 감소한다.
- Provider는 `ApplyEffectAction`, `DamageAction` 또는 필요한 기존 Action의 순서를 반환한다.

마레아 액티브는 피해 Action이 아니라 물속성 피해 증가 `ApplyEffectAction`을 만든다. `EffectModifierType.ElementDamageIncrease`, 물속성 필터, `EffectStackPolicy.RefreshDuration`을 사용하며, 동일 턴 뒤이어 실행되는 물속성 공격도 실행 시점의 효과 수집에서 보정을 받는다.

유한 지속 효과는 `BattleTurnEndProcessor`가 캐릭터, 파티, 보스 Effect와 실드를 턴 종료마다 처리한다. “3턴” 효과는 적용 직후부터 세 번째 턴 종료 처리 전까지 유효하고, 세 번째 종료 처리에서 만료되는 현재 테스트 계약을 따른다. 신규 효과의 재적용 정책과 크기 변경 허용 여부는 `EffectCollection` 정책에 맞춰 테스트한다.

각성으로 액티브 수치나 동작이 달라지면 Config를 직접 여러 곳에서 분기하기보다 캐릭터 규칙 해석 결과를 Provider에 주입한다. 액티브 ID, 쿨타임, Action ID 계보는 각성 여부와 무관하게 기존 실행 계약을 지킨다.

## 제6장. Passive 및 Trigger Rule

### 상시 보정과 이벤트 Trigger

- 현재 공격의 조건을 보고 피해율만 바꾸는 상시 패시브는 Provider의 `ActionLocalDamageIncreaseRate` 또는 기존 Modifier 수집 구조에 둔다.
- 다른 Action의 완료, 실제 자원 소비, 특정 결과에 반응하여 새 Action을 만들 때 `ICombatTriggerRule`을 사용한다.

마레아의 “물 원소가 있으면 피해 증가”는 Match 공격 시작 조건을 Provider가 확정하는 상시 보정 사례다. 다른 아군의 물 원소 소비에 반응하는 패시브처럼 완료 결과가 필요한 규칙은 Trigger 사례다.

### 자원 소비 Trigger 표준

1. `CombatActionTriggerContext.ActionResult`가 `ConsumeResourceActionResult`인지 확인한다.
2. `ConsumptionRecord`가 null이 아닌지 확인한다. 요청량이 아니라 `ConsumedAmount`를 사용한다.
3. `ResourceId`, 대상, 소비 주체를 검증한다.
4. “다른 아군” 조건이면 `ConsumerId`와 Trigger 소유 캐릭터 ID가 다른지 확인한다.
5. 조건이 맞으면 `context.ActionIds.Next()`로 파생 Action ID를 발급한다.
6. 파생 Action의 `RootActionId`는 `context.RootActionId`, `SourceActionId`는 `context.ActionId`로 둔다.

Registrar가 만든 Trigger Rule 인스턴스를 `CharacterCombatProviderRegistrationResult.TriggerRules`로 반환하면, `BattleSceneCombatComposition`이 실제 파티 슬롯 순서대로 수집해 Resolver에 전달한다. 파티에 없는 캐릭터의 Registrar는 실행되지 않으므로 Rule도 등록되지 않는다. Factory 기반 Registrar 생성으로 전투 사이에 Rule 인스턴스 상태를 공유하지 않는다.

### 후속 연쇄와 종료 규칙

`CanTriggerFollowUp == false`는 모든 Rule 평가를 건너뛴다는 뜻이 아니다. `CombatTriggerResolver`는 완료된 Action을 Rule에 전달한다. 다만 반환된 `DamageAction`이 Additional 또는 Chase인데 `context.CanTriggerAttackFollowUp`이 false이면 거부한다. 따라서 Additional/Chase가 또 다른 공격형 Additional/Chase를 무한히 잇는 구조는 허용되지 않는다.

비공격형 파생 Action은 이 공격 후속 제한만으로 일괄 거부되지 않는다. 그래도 모든 파생 Action은 Resolver의 root/source 계보와 ID 중복 검증을 통과해야 하며, 개별 Action 생성자의 Origin 정책도 만족해야 한다. 이 규칙을 “모든 비공격 Trigger가 언제나 허용된다”로 확대 해석하지 않는다.

`ConsumeResourceActionResult`의 공격 후속 가능 여부는 소비 Action 자체가 아니라 연결된 source Action 결과를 기준으로 계산된다. 이로써 Match/Active에서 파생된 실제 소비가 한 번의 공격형 추격을 만들 수 있고, 이미 Additional/Chase에서 시작한 연쇄는 막힌다.

Action 실행 후 보스가 사망하거나 파티가 전멸하면 `CombatActionExecutionSession`은 큐를 비우고 Trigger를 평가하지 않는다. Trigger Rule 안에서 별도 전투 종료 우회 경로를 만들지 않는다.

## 제7장. Additional 및 Chase 공격

| 구분 | `ActionOrigin` | `AttackType` | 일반적인 source | 새 공격형 후속 허용 |
|---|---|---|---|---|
| 기본 매칭 | `Match` | `Match` | 없음(자기 자신이 root) | 예 |
| 별도 추가 공격 | `Additional` | `Additional` | 원인이 된 Match/Active 계열 Action | 아니오 |
| 추격 | `Chase` | `Chase` | 조건을 발생시킨 완료 Action | 아니오 |

모든 전투 Action은 Composition이 만든 동일한 `CombatActionIdSequence`를 사용한다. Root Action은 별도 source가 없고 자신의 ID가 root다. 파생 Action은 전체 계보의 시작인 `RootActionId`와 직접 원인인 `SourceActionId`를 모두 보존한다.

공격 유형별 피해 증가는 `DamageContextFactory`가 각 DamageAction 실행 시 `AttackType`과 태그를 기준으로 수집한다. Additional과 Chase를 Match 계수에 합쳐 버리면 해당 유형 전용 효과를 독립 적용할 수 없으므로, 설계가 별도 공격을 뜻할 때만 Action을 분리한다.

### ID 순서와 실행 순서

Provider가 `[Match(id=1), Additional(id=2)]`를 반환한 뒤 Match 완료 Trigger가 파생 Action `(id=3)`을 만들 수 있다. `CombatActionExecutionSession`은 Resolver 결과를 `CombatActionQueue.EnqueueNextRange`로 현재 큐 앞에 넣고, 목록 내부 순서는 유지한다. 실제 순서는 다음과 같다.

```text
발급 순서: Match(1) → Provider Additional(2) → Trigger derived(3)
실행 순서: Match(1) → Trigger derived(3) → Provider Additional(2)
```

따라서 테스트는 단순 ID 오름차순을 실행 순서로 가정하지 말고, 각 항목의 클래스·Origin·AttackType·Root·Source를 검증한다. 치명타 발생은 실행 중단 조건이 아니다. 실제 보스 사망 또는 파티 전멸만 남은 큐와 Trigger 처리를 중단한다.

## 제8장. 공유 전투 자원

공유 자원의 존재와 상한은 `Assets/Game/Battle/Combat/Integration/CombatResourceRequirement.cs`로 선언한다. 캐릭터 Registrar는 자원을 직접 등록하지 않고 `ResourceRequirements`에 요구사항을 반환한다.

Composition은 실제 파티 Registrar의 요구사항만 모아 `CombatResourceRequirementMerger.Merge`에 전달한다.

- 같은 ID와 같은 상한은 하나로 병합한다.
- 같은 ID에 다른 상한이 들어오면 명확한 오류로 거부한다.
- 병합 결과는 Resource ID 기준의 결정적인 순서를 가진다.
- 이미 `ResourceCollection`에 등록된 ID를 다시 등록하지 않는다.
- 해당 자원을 요구하는 캐릭터가 파티에 없으면 그 요구사항으로 자원을 만들지 않는다.

마레아 Registrar는 `WaterElementResource.ResourceId`와 `WaterElementResource.MaxAmount`를 요구한다. 앞으로 같은 물 원소를 쓰는 캐릭터도 동일 요구사항을 반환해야 하며, 마레아의 존재에 의존해서는 안 된다.

책임을 분리한다.

- Registrar/Composition: 자원의 존재와 상한 보장.
- Match/Active Provider: `AddResourceAction`, `ConsumeResourceAction`으로 획득·소비 요청.
- Executor/ResourceCollection: 실제 증감, 상한 적용, 실제 소비 결과 생성.
- Trigger Rule: `ResourceConsumptionRecord`를 읽어 후속 행동 판단.

새 공용 자원을 추가할 때 기존 `WaterElementResource`와 `CombatResourceRequirementMerger`의 계약을 재사용한다. 캐릭터별로 별도 ResourceCollection이나 중복 등록 코드를 만들지 않는다.

## 제9장. Combat Registrar

`Assets/Game/Battle/Combat/Integration/ICharacterCombatProviderRegistrar.cs`를 구현한다. Registrar는 다음 정보를 선언한다.

- `CharacterId`
- 지원하는 `CombatConfigDefinition` 타입
- `Register(...)`를 통한 Match/Active Provider 등록
- Active ID와 쿨타임
- Trigger Rule 목록
- 공유 자원 요구사항 목록
- HUD가 읽을 캐릭터 고유 전투 상태 목록

`Register`가 받는 `CombatActionIdSequence`는 전투 전체의 공용 인스턴스다. Registrar나 Provider가 새 Sequence를 만들지 않는다. 파티원 `CharacterBattleState`, 공용 `PartyBattleState`, `BossBattleState`를 현재 전투 인스턴스 그대로 Provider/Rule에 연결한다.

마레아 예시는 `Assets/Game/Characters/Marea/MareaBluefangCombatProviderRegistrar.cs`와 `MareaBluefangCombatProviderRegistration.cs`다. Match와 Active Provider를 Registry에 등록한 뒤 `CharacterCombatProviderRegistrationResult.WithActiveAbility(...)`로 액티브 정보, 빈 Trigger 목록, 물 원소 요구사항을 반환한다.

등록 결과의 목록은 null일 수 없고 null 요소를 포함할 수 없다. 액티브가 없는 캐릭터는 `WithoutActiveAbility` 경로를 사용한다. Registrar는 Resolver 생성 순서, ResourceCollection 병합, 액션 큐 처리 같은 Composition 책임을 직접 수행하지 않는다.

여러 Provider와 Trigger가 같은 캐릭터 고유 전투 상태를 사용한다면 Registrar가 전투마다 상태 인스턴스를 정확히 하나 생성해 공유한다. HUD에 보여야 하는 횟수형 상태는 별도 UI 복사본을 만들지 않고 같은 인스턴스를 읽기 전용 `ICharacterCombatStatusSource`로 등록 결과에 제공한다. Composition은 이 목록을 현재 전투에만 보유하므로 새 전투에는 이전 상태가 전달되지 않는다.

## 제10장. 런타임 코드 등록

코드 측 단일 등록 지점은 `Assets/Game/Characters/Runtime/CharacterRuntimeRegistrationCatalog.cs`의 `CreateDefault()`다. 한 등록 항목은 `CharacterRuntimeRegistration`으로 다음 Factory를 묶는다.

- `ICharacterBuildModifier`
- `ICharacterCombatProviderRegistrar`
- `ICharacterLookupDescriptionProvider`

Factory를 저장하는 이유는 Resolver/전투마다 새 인스턴스를 만들어 상태 공유를 방지하기 위해서다. 생성된 세 구성 요소의 CharacterId가 등록 ID와 같은지, CombatConfig 타입이 다른 캐릭터 Registrar와 충돌하지 않는지 Catalog가 검증한다.

신규 캐릭터를 추가할 때 코드 등록을 위해 수정할 기존 파일은 원칙적으로 `CharacterRuntimeRegistrationCatalog.cs` 하나다. 다음 경로는 이미 Catalog의 투영 결과를 사용하므로 캐릭터별 분기를 추가하지 않는다.

- `Assets/Game/Characters/Build/CharacterBuildResolverFactory.cs`
- `Assets/Game/Characters/Presentation/CharacterLookupDescriptionResolverFactory.cs`
- `Assets/Game/Battle/Flow/Presentation/BattleSceneCombatBootstrap.cs`

현재 기본 Catalog에 들어 있는 마레아 등록은 다음과 같다. 신규 캐릭터도 같은 생성자와 Factory 형태로 항목을 추가한다.

```csharp
new CharacterRuntimeRegistration(
    MareaBluefangRules.CharacterId,
    () => new MareaBluefangBuildModifier(),
    () => new MareaBluefangCombatProviderRegistrar(),
    () => new MareaBluefangLookupDescriptionProvider())
```

신규 항목에서는 위 마레아 타입을 해당 캐릭터의 실제 구현 타입으로 대체한다. 인스턴스를 미리 만들어 람다 밖에서 공유하지 않는다. 등록 후 Catalog 테스트로 기본 목록에 정확히 한 번 포함되는지와 세 투영 경로를 확인한다.

## 제11장. ScriptableObject 및 UI 등록

코드 구현과 별도로 다음 콘텐츠를 연결한다.

1. 캐릭터별 CombatConfig 에셋을 만든다.
2. `CharacterDefinition`에 ID, 원소, 기본 능력치, 스킬 ID, CombatConfig를 연결한다.
3. Match/Active/Passive 등 표시가 필요한 `SkillDefinition`과 아이콘·Localization 키를 만든다.
4. Character와 Skill 에셋을 `Assets/Game/Data/Database/DefinitionDatabase.cs`가 참조하는 Database 에셋에 등록한다.
5. `CharacterPresentationDefinition`에 같은 CharacterId와 `FaceSprite`를 설정하고 `CharacterPresentationCatalog`에 등록한다.
6. 필요한 Effect가 정의 기반 표시를 요구하면 `EffectDefinition`과 Database 참조를 연결한다.
7. 실제 획득 가능 정책에 따라 `GachaDefinition.CharacterPoolIds` 또는 `ExchangeCatalogDefinition.CharacterIds`에 포함할지 결정한다.

`Assets/Game/Battle/Flow/Presentation/BattleHudController.cs`는 실제 파티 슬롯의 CharacterId로 `CharacterPresentationCatalog`를 조회해 FaceSprite를 `BattleCharacterSlotView`에 전달한다. 캐릭터가 바뀌거나 정의/스프라이트가 누락되면 이전 이미지를 지우고 전투 기능은 계속 유지한다. 따라서 신규 캐릭터마다 HUD Controller에 ID 분기나 Sprite 하드코딩을 추가하지 않는다.

캐릭터 상태 슬롯에는 `EffectInstance`를 먼저 생성 순서대로 표시하고, 남은 슬롯에 현재 캐릭터와 파티 슬롯이 일치하는 읽기 전용 전투 상태를 표시한다. 상태 슬롯 객체는 특정 Effect나 상태 전용이 아니므로 앞의 Effect가 만료되면 뒤의 상태가 앞쪽 빈 슬롯으로 이동할 수 있다. 현재 캐릭터별 상태 슬롯은 4개이며 Effect가 모두 차지하면 전투 상태는 원본 값을 유지한 채 화면에만 표시되지 않는다. 이 제한을 바꾸려면 별도의 UX 정책 결정이 필요하다.

Sprite 참조는 GUID 존재 여부만으로 검증하지 않는다. 실제 에셋을 `AssetDatabase`로 로드해 참조가 null이 아닌지, 기대 경로의 단일 Sprite인지 확인한다. Inspector에서 연결한 `fileID`가 잘못되면 GUID가 유효해도 런타임 참조가 비어 있을 수 있다.

Localization 키와 아이콘은 기존 에셋 명명·등록 규칙을 따른다. ID가 같더라도 CharacterDefinition, RuntimeRegistration, Presentation, Gacha/Exchange 참조 중 하나가 빠지면 플레이 가능 캐릭터로 완성된 것이 아니다.

## 제12장. 등록 무결성 검증

검증 도구의 책임은 다음과 같다.

- `Assets/Game/Data/Validation/DataValidator.cs`
  - Definition ID와 중복, 참조 무결성, Character CombatConfig, 스킬 참조, 가챠 풀, 교환소 ID 등 일반 콘텐츠 데이터를 검증한다.
- `Assets/Game/Tests/EditMode/Characters/Runtime/CharacterRuntimeRegistrationCatalogTests.cs`
  - 런타임 등록 중복, 구성 요소 ID 일치, CombatConfig 타입 충돌, 세 Factory 투영, 인스턴스 독립성을 검증한다.
- `Assets/Game/Tests/EditMode/Characters/Runtime/CharacterContentRegistrationValidationTests.cs`
  - Editor 전용 `CharacterContentRegistrationValidator`를 통해 코드·Definition·Presentation·획득 경로의 교차 등록을 검증한다.

현재 플레이 가능 캐릭터 판정 집합은 다음 합집합이다.

```text
RuntimeRegistration의 CharacterId
∪ 모든 GachaDefinition.characterPoolIds
∪ ExchangeCatalogDefinition.characterIds
```

이 집합의 캐릭터는 CharacterDefinition, RuntimeRegistration, 올바른 CombatConfig 타입, 유효한 SkillDefinition 참조, PresentationDefinition과 비어 있지 않은 FaceSprite를 갖춰야 한다. 미완성 개발용 정의가 위 세 경로 어디에도 포함되지 않으면 완성된 전투 등록을 강제하지 않는다. 반대로 개발 중 캐릭터를 RuntimeRegistration에 먼저 넣으면 플레이 가능 대상으로 간주되므로 관련 콘텐츠를 함께 준비하거나 등록 시점을 늦춘다.

향후 이벤트 보상, 상점 등 새로운 획득 경로가 생기면 해당 ID 공급원을 `CharacterContentRegistrationValidator`의 플레이 가능 집합 정책에 추가해야 한다. 자동 검증이 새 획득 경로를 저절로 발견한다고 가정하지 않는다.

## 제13장. 테스트 작성 표준

### 캐릭터 고유 테스트

- Build/각성: 레벨 경계, 각성 0~6, 영구 HP/ATK와 전투 계수의 책임 분리.
- CombatConfig: 기본값과 모든 잘못된 범위에 대한 `TryValidate`.
- Match Provider: 3/4/5매칭, 조건부 계수, 자원 snapshot, 피해 전후 증감, 최대 상한, Action ID와 root/source.
- Active Provider: 액션 종류, 원소/유형 보정, 재적용 정책, 정확한 만료 턴, 사용 직후 쿨타임과 턴별 감소.
- Passive/Trigger: 조건 충족·불충족, 실제 소비량 0/1/복수, 자기 소비 제외, 여러 이벤트, 파티 미편성, 전투 간 상태 격리.
- Additional/Chase: 각 DamageAction의 Origin/AttackType, 유형별 버프 독립 적용, 계보, Trigger 우선 실행, 후속 공격 연쇄 거부.
- Registrar: Provider 등록, 액티브 정보, Trigger/Resource 기여, 공용 Action ID Sequence 사용.
- Composition 통합: 실제 Catalog와 Registrar를 거쳐 Action이 실행되고 보스 HP·피해 점수·자원·쿨타임에 반영되는지 검증.

마레아 기준 파일은 다음과 같다.

- `Assets/Game/Tests/EditMode/Characters/Marea/MareaBluefangMatchActionProviderTests.cs`
- `Assets/Game/Tests/EditMode/Characters/Marea/MareaBluefangActiveActionProviderTests.cs`
- `Assets/Game/Tests/EditMode/Characters/Marea/MareaBluefangAwakeningCombatTests.cs`
- `Assets/Game/Tests/EditMode/Battle/Combat/Integration/MareaBluefangBattleFlowIntegrationTests.cs`
- `Assets/Game/Tests/PlayMode/BattleSceneCombatPlayModeTests.cs`

### 공통 테스트와 중복 방지

ID 발급 일반 규칙, Queue FIFO/`EnqueueNextRange`, Resolver의 일반 계보 검증, Resource 병합, DamageCalculator 공식 자체는 기존 공통 테스트가 담당한다. 신규 캐릭터 테스트는 그 공통 기능을 다시 전부 복제하지 말고, 캐릭터가 올바른 입력과 Action을 만들어 공통 경로에 전달하는지를 검증한다.

관련 공통 기준선은 `MatchEventCombatActionFactoryTests`, `CombatActionExecutionSessionTests`, `CombatTriggerResolverTests`, `CombatResourceRequirementMergerTests`, `BattleResultRuntimeIntegrationTests`, `BattleHudControllerTests`, Runtime/Content registration 테스트다.

### UI·PlayMode·수동 확인

- EditMode: Presentation Catalog 조회, CharacterId별 다른 Sprite, null/누락 시 잔상 제거, 액티브/쿨타임/원소 아이콘 회귀.
- PlayMode: 실제 `Battle.unity` Composition, 파티 슬롯 순서와 빈 슬롯, 액티브 클릭, 보스 사망 중단, 전투 결과 반영, Effect와 캐릭터 고유 상태의 슬롯 이동을 검증한다.
- 수동 Editor: 실제 에셋 참조, Localization 표시, FaceSprite와 스킬 아이콘, 1080×1920 및 Safe Area, 캐릭터를 포함/제외·순서 변경한 파티를 확인한다.

자동 테스트를 실행하지 못했으면 정적 검토와 구분해 미실행으로 보고한다. 치명타 때문에 후속 Action이 중단되었다고 가정하지 말고, 보스 실제 사망 여부를 별도로 제어한다.

`CombatActionStepApplied`는 해당 Action 실행과 Trigger 처리가 끝난 뒤 발생한다. HUD는 이 이벤트에서 원본 Effect와 전투 상태를 다시 읽어야 하며, 프레임 폴링이나 별도 표시용 상태를 추가하지 않는다. 테스트도 Action 결과만이 아니라 Trigger 완료 직후의 원본 상태와 실제 슬롯 내용을 함께 검증한다.

## 제14장. 신규 캐릭터 구현 체크리스트

### 설계와 데이터

- [ ] 안정적인 CharacterId를 확정했다.
- [ ] 기본 원소와 레벨 1/100 HP·ATK를 확정했다.
- [ ] 3/4/5매칭, 액티브, 패시브, 각성 규칙의 권위 있는 Config 필드를 정의했다.
- [ ] CombatConfig 구현과 `TryValidate`를 완료했다.
- [ ] CharacterDefinition과 SkillDefinition 에셋을 만들고 연결했다.
- [ ] 각성 효과마다 Build/Provider/Trigger 중 단 하나의 책임 위치를 정했다.

### 캐릭터 전용 코드

- [ ] Build Modifier를 구현했다.
- [ ] 필요 시 각성별 Combat Rule Resolver를 구현했다.
- [ ] Match Provider를 구현했다.
- [ ] Active Provider 또는 “액티브 없음” 계약을 구현했다.
- [ ] 상시 Passive와 이벤트 Trigger Rule을 구분해 구현했다.
- [ ] Additional/Chase가 있다면 별도 DamageAction, 유형, 계보, 연쇄 제한을 검증했다.
- [ ] 공유 자원 요구사항과 획득·소비 Action의 책임을 분리했다.
- [ ] Combat Registrar가 Provider, 액티브 정보, TriggerRules, ResourceRequirements를 반환한다.
- [ ] 모든 Action이 전투 공용 `CombatActionIdSequence`를 사용한다.
- [ ] `CharacterRuntimeRegistrationCatalog.CreateDefault()`에 Factory 기반 등록을 정확히 한 번 추가했다.
- [ ] Build Modifier, Registrar, Lookup Provider의 CharacterId가 모두 같다.

### 콘텐츠와 표시

- [ ] DefinitionDatabase에 Character/Skill/CombatConfig 관련 참조를 등록했다.
- [ ] CharacterPresentationDefinition과 FaceSprite를 등록했다.
- [ ] CharacterPresentationCatalog에 같은 CharacterId로 등록했다.
- [ ] 표시명·스킬명·설명 Localization을 등록했다.
- [ ] 필요한 스킬·효과·원소 UI 아이콘과 EffectDefinition을 연결했다.
- [ ] 가챠·교환소 포함 여부를 명시적으로 결정했다.
- [ ] HUD나 Composition에 캐릭터별 하드코딩을 추가하지 않았다.

### 테스트와 실제 확인

- [ ] CombatConfig, Build, 각성 테스트가 통과한다.
- [ ] Match/Active/Passive/Trigger 개별 테스트가 통과한다.
- [ ] 자원 획득·소비·상한·실제 소비 기록 테스트가 통과한다.
- [ ] Additional/Chase 유형별 버프, root/source, 실행 순서, 연쇄 제한 테스트가 통과한다.
- [ ] 실제 Composition 통합 테스트에서 HP·피해 점수·쿨타임·자원 결과가 정확하다.
- [ ] Runtime registration과 콘텐츠 등록 검증이 통과한다.
- [ ] 기존 공통 EditMode 회귀 테스트가 통과한다.
- [ ] 관련 PlayMode 테스트가 통과한다.
- [ ] 실제 Battle 화면에서 이미지, 원소, 액티브, 쿨타임을 확인했다.
- [ ] 파티 미편성, 단독 편성, 다른 캐릭터와 동시 편성, 슬롯 순서 변경을 확인했다.
- [ ] 보스가 중간 Action에서 사망할 때 남은 Action/Trigger가 중단되는지 확인했다.

---

## 현재 구현과 설계 문서 차이 및 세레나 구현에서 확인된 주의사항

### 확인된 차이

1. 마레아 설계 문서의 4매칭 “추가 피해”는 현재 코드에서 별도 Additional 공격이 아니다. `MareaBluefangMatchActionProvider`가 조건부 계수를 합산한 단일 Match DamageAction을 만든다.
2. 물 원소 상한은 단순한 마레아 전용 값이 아니다. 현재 코드는 `WaterElementResource.MaxAmount` 5를 공용 계약으로 두고 Marea Config가 이에 일치하는지 검증한다.
3. 설계 문서는 주로 결과와 의도를 설명하지만, 실제 Action 실행은 피해·자원 소비·Trigger를 별도 Action과 결과로 나눈다. 구현과 테스트는 이 실제 순서 및 계보를 기준으로 한다.

### 세레나 구현에서 확인된 재사용 규칙

- 별도 추가 공격은 `AttackType.Additional`, 소비 이벤트 기반 추격은 `AttackType.Chase`로 생성한다. 공격 유형별 Effect 보정과 Action 로컬 보정은 `DamageContextFactory`가 같은 피해 증가 계열에서 합산하며 `DamageCalculator` 공식은 변경하지 않는다.
- 실제 자원 소비 반응은 요청량이 아니라 `ResourceConsumptionRecord.ConsumedAmount`를 기준으로 한다. 자기 소비를 제외하고 소비 이벤트당 Chase 하나를 만들며 Additional/Chase에서 시작한 공격형 연쇄는 차단한다.
- 횟수형 강화 상태는 프로필이나 공유 Resource가 아니라 Registrar가 전투마다 만든 캐릭터 고유 BattleState에 둔다. 획득 Trigger, 소비 Trigger, 읽기 전용 StatusSource가 같은 인스턴스를 사용한다.
- RuntimeRegistration은 Build, Combat, Lookup 코드 경로를 여는 등록이며 가챠·교환소 편입과 독립적이다. 획득 경로에 넣지 않은 캐릭터를 Catalog Builder가 자동 추가해서는 안 된다.
- HUD는 Effect를 먼저, 캐릭터 고유 상태를 남은 슬롯에 표시한다. 슬롯은 표시 위치일 뿐 특정 상태의 영구 소유물이 아니므로 Effect 만료 뒤 상태 아이콘이 앞쪽으로 이동할 수 있다.
- 보스 사망은 남은 Action과 Trigger를 중단한다. 강화 Chase 자체가 보스를 처치하면 그 Chase의 완료 Trigger가 생략되어 종료 직후 BattleState에 예약된 횟수가 남을 수 있지만, 종료된 Composition과 함께 폐기된다. 이 값을 영구 데이터나 다음 전투로 전달하지 않는다.
- 세레나 전용 동작은 캐릭터 Provider와 Trigger에 머물러야 한다. 위 패턴을 재사용하더라도 세레나의 계수, 강화 추격 의미, HUD 상태 ID를 공통 시스템 규칙으로 일반화하지 않는다.

