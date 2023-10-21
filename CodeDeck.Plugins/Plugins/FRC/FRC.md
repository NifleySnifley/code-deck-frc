# FRC

The FRC plugin is at the heart of frc-deck, providing tiles that interface with the NetworkTables to display and create data.

## NTBooleanButton

The `NTBooleanButton` tile can act as either a push or toggle button, setting a boolean value on the NetworkTables

### Settings

| Setting    | Default           | Description                                                             |
| ---------- | ----------------- | ----------------------------------------------------------------------- |
| NTPath     | `NTBooleanButton` | The path to the boolean value of the button on NetworkTables            |
| TrueColor  | `transparent`     | Background color of the button while `true`                             |
| FalseColor | `transparent`     | Background color of the button while `false`                            |
| Toggle     | `false`           | Whether the button is a push button (`false`) or toggle button (`true`) |

#### Examples

A toggle button posting its value to `Shuffleboard/bool1`:

```json
    {
        "Plugin": "FRC",
        "Tile": "NTBooleanButton",
        "Settings": {
            "NTPath": "Shuffleboard/bool1",
            "Toggle": "true",
            "TrueColor": "green",
            "FalseColor": "red"
        }
    }
```

## NTBooleanIndicator

The `NTBooleanIndicator` tile displays a boolean value from the NetworkTables

### Settings

| Setting    | Default           | Description                                                    |
| ---------- | ----------------- | -------------------------------------------------------------- |
| NTPath     | `NTBooleanButton` | The path to the boolean value to be displayed on NetworkTables |
| TrueColor  | `green`           | Background color of the indicator when `true`                  |
| FalseColor | `red`             | Background color of the indicator when `false`                 |
| TrueImage  | `null`            | The indicator's icon image when `true`                         |
| FalseImage | `null`            | The indicator's icon image when `false`                        |

#### Examples


