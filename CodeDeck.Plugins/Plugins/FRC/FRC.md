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
| TrueText   | `""`              | The text of the indicator when `true`                                   |
| FalseText  | `""`              | The text of the indicator when `false`                                  |

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

| Setting    | Default              | Description                                                    |
| ---------- | -------------------- | -------------------------------------------------------------- |
| NTPath     | `NTBooleanIndicator` | The path to the boolean value to be displayed on NetworkTables |
| TrueColor  | `green`              | Background color of the indicator when `true`                  |
| FalseColor | `red`                | Background color of the indicator when `false`                 |
| TrueImage  | `null`               | The indicator's icon image when `true`                         |
| FalseImage | `null`               | The indicator's icon image when `false`                        |
| TrueText   | `""`                 | The text of the indicator when `true`                          |
| FalseText  | `""`                 | The text of the indicator when `false`                         |

#### Examples

A basic indicator showing the state of `Shuffleboard/bool1` with the default colors and added text.

```json
{
    "Plugin": "FRC",
    "Tile": "NTBooleanIndicator",
    "KeyType": "Normal",
    "Settings": {
        "NTPath": "Shuffleboard/bool1",
        "TrueText": "TRUE",
        "FalseText": "FALSE"
    }
}
```

## NTValueSetter

The `NTBooleanIndicator` tile sets a certain NetworkTables entry to a specific value when pressed. Additionally, the tile sets its display style based on whether the NetworkTables' entry is the equal to the button's value. The most obvious use case for this is to create radio buttons.

### Settings

| Setting | Default         | Description                                                    |
| ------- | --------------- | -------------------------------------------------------------- |
| NTPath  | `NTValueSetter` | The path to the value to be set & compared to on NetworkTables |

#### Examples
