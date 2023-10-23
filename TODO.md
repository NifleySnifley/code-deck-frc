## TODO:

### FRC Plugin

#### Tiles

- [x] Boolean push/toggle button `NTBooleanButton`
- [x] Boolean indicator `NTBooleanIndicator`
- [x] Radio button/smart value setter `NTValueSetter`
- [x] Integer add/subtract `NTNumberChanger`
- [ ] Text/Value display tile `NTTextDisplay`
- [ ] Number display tile (selectable style: bargraph, dial, full-tile bargraph, etc.) `NTNumberDisplay`
  - [ ] Themes
- [x] Enum display (value -> bg color and icon image) `NTValueIndicator`
  - [x] Each value is associated with color, image, text, etc.
  - [ ] Make `NTBooleanIndicator` a subclass of this
  - [ ] Plugin-global enum definitions?
- [ ] Multi-value toggle `NTMultiSelector`
- [ ] SendableChooser interface? 

#### FRC Plugin Docs:

- [x] `NTBooleanButton`
- [x] `NTBooleanIndicator`
- [x] `NTValueSetter`
- [x] `NTNumberChanger`
- [ ] `NTTextDisplay`
- [ ] `NTNumberDisplay`
- [x] `NTValueIndicator`
- [ ] `NTMultiSelector`

### General Things

- [ ] Proper CLI argument handling
- [ ] Loading images relative to config directory
- [x] Class for general style (Color, Text, Image, TextColor)
  - [x] Method to store tile's default (base) style
  - [x] Other styles are parsed in addition to the default
  - [x] Other styles are added on to the default
  - [x] FIX and make it actually work!!!
- [ ] Add images to radio buttons based on selection state??
- [ ] Fix NT state updaters being called a ridiculous amount of times!!!
