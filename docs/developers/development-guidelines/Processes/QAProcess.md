# QA Process

## 1. Ticket Requirements
#### For all tickets:
- Definitions of Done (DoD) must be included in every ticket.
    - Clear acceptance criteria.
- Expected behavior.
- Design references (if applicable).

#### For smaller fixes:
- Clearly outline how the fix should behave.
- DoD focused on specific outputs/results.
#### For complex features:
- Require detailed design documents and behavior specifications.
- Extensive DoD, including success criteria for each component.
- Additional documentation for edge cases, user flows, and dependencies.

## 2. Testing Approach
### Iteration Testing
Focus on smaller tickets:
- Test individual components or fixes as they are developed.
- Validate against the DoD and expected behavior.
- Perform regression testing to ensure no impact on existing features.

### Full QA for Larger Features (Feature Complete Testing)
Once all smaller tickets are completed for a larger feature:
- Execute a Full QA Process:
- End-to-end testing of the entire feature.
- Validate all integrated parts function together correctly.
- Test across relevant platforms, environments, or user roles.
- Perform usability and edge case testing.
- Ensure feature meets overall DoD for the project/epic.
- Sign-off required before release.