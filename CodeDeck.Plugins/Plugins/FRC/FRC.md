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

The `NTValueSetter` tile sets a certain NetworkTables entry to a specific value when pressed. Additionally, the tile sets its display style based on whether the NetworkTables' entry is the equal to the button's value. The most obvious use case for this is to create radio buttons.

### Settings

| Setting         | Default         | Description                                                                                                                                       |
| --------------- | --------------- | ------------------------------------------------------------------------------------------------------------------------------------------------- |
| NTPath          | `NTValueSetter` | The path to the value to be set & compared to on NetworkTables                                                                                    |
| Value           | `unassigned`    | The value that the NetworkTables entry is set to when the button is pressed, and the value to be compared against for setting the button's style. |
| Initial         | `null`          | The value that the NetworkTables entry is set to when the button is initialized                                                                   |
| SelectedColor   | `"green"`       | The color of the button when the NetworkTables entry **is equal** to the button's value                                                           |
| UnselectedColor | `"transparent"` | The color of the button when the NetworkTables entry **is not equal** to the button's value                                                       |

#### Examples

Three "radio buttons" with the values 1, 2, and 3. 

```json
{
    "ID": "0,0",
    "Plugin": "FRC",
    "Tile": "NTValueSetter",
    "Text": "1",
    "Settings": {
        "NTPath": "TestRadio1",
        "Value": 1
    }
},
{
    "ID": "0,1",
    "Plugin": "FRC",
    "Tile": "NTValueSetter",
    "Text": "2",
    "Settings": {
        "NTPath": "TestRadio1",
        "Value": 2
    }
},
{
    "ID": "0,2",
    "Plugin": "FRC",
    "Tile": "NTValueSetter",
    "Text": "3",
    "Settings": {
        "NTPath": "TestRadio1",
        "Value": 3,
        "Initial": 2
    }
}
```

## NTNumberChanger

The `NTNumberChanger` tile adds a certain value to a numerical NetworkTables entry each time it is pressed within configurable bounds. Additionally, the `NTNumberChanger` tile can be configured to repeatedly change the numeric NetworkTables entry when held. The visual style of the `NTNumberChanger` changes based on whether the NetworkTables entry has reached or exceeded a configurable maximum of minimum value.

### Settings

| Setting     | Default           | Description                                                                                                                                                        |
| ----------- | ----------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| NTPath      | `NTNumberChanger` | The path to NetworkTables entry                                                                                                                                    |
| Max         | `Double.MaxValue` | Maximum value of the NetworkTables entry                                                                                                                           |
| Min         | `Double.MinValue` | Minimum value of the NetworkTables entry                                                                                                                           |
| Increment   | `0`               | The amount **added** to the NetworkTables entry every time the button is pressed *and* every `HoldPulseMS` milliseconds when the button is held if `Holdable`      |
| Holdable    | `false`           | if `true` the button will repeatedly add to the NetworkTables entry when held, if `false`, the button will only add to the NetworkTables entry when first pressed. |
| HoldPulseMS | `500`             | The button will add `Increment` to the NetworkTables entry every `HoldPulseMS` milliseconds **if** `Holdable == true`                                              |
| HoldSenseMS | `500`             | The time to wait (milliseconds) before the button will start repeatedly adding to the NetworkTables entry. (debounce)                                              |
| MaxColor    | `null`            | The color of the button when `value >= Max`                                                                                                                        |
| MinColor    | `null`            | The color of the button when `value <= Max`                                                                                                                        |

#### Examples

A button that increments `TestNumChanger1` up to a max value of `10`, turning `red` when it reaches `10`.

```json
{
    "Plugin": "FRC",
    "Tile": "NTNumberChanger",
    "Text": "+1",
    "Settings": {
        "NTPath": "TestNumChanger1",
        "Increment": 1,
        "Max": 10,
        "MaxColor": "red"
    }
}
```

## NTValueIndicator

The `NTValueIndicator` tile sets it's entire appearance based on the value of a NetworkTables entry. 

### Settings

| Setting | Default            | Description                                          |
| ------- | ------------------ | ---------------------------------------------------- |
| NTPath  | `NTValueIndicator` | The path to the NetworkTables entry                  |
| Styles  | `{ Value: Style }` | A JSON map associating values with `Style` settings. |

### Style

Each `Style` is a JSON object with the following intuitively named parameters:

- `BackgroundColor`

- `TextColor`

- `Image`  (path to the image as a string)

- `FontSize`

- `Font`

**Note**: Values (keys) must be literal representations of the actual value due to parsing limitations. For instance the numerical value of `1` should be specified as `"1"` and the string value of the character `1` should be specified as `"\"1\"" `.

#### Examples

Displaying the state of the radio buttons in the example from `NTValueSetter` above using colored text.

```json
{
    "Plugin": "FRC",
    "Tile": "NTValueIndicator",
    "KeyType": "Normal",
    "Settings": {
        "NTPath": "TestRadio1",
        "Styles": {
            "1": {
                "Text": "1",
                "TextColor": "red"
            },
            "2": {
                "Text": "2",
                "TextColor": "orange"
            },
            "3": {
                "Text": "3",
                "TextColor": "yellow"
            }
        }
    }
}
```
