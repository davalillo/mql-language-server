// MQL5 fixture: #resource directive + pack pragma
// Covers Mql5Grammar constructs: #resource, pack(n)
#property copyright "MQL5 fixture"
#resource "res/icon.bmp" as bitmap MyIcon;

#pragma pack(8)
class PackedData {
    int value;
    double rate;
};

void OnTick() {
    PackedData data;
}
