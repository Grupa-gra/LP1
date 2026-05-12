<div align="center">

# Mushroom Mania

![Unity](https://img.shields.io/badge/Unity-6000.0.24f1-000000?style=for-the-badge&logo=unity&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![Status](https://img.shields.io/badge/Status-Educational-blueviolet?style=for-the-badge)
![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)

</div>

---

## Wersja Polska

### O Projekcie
**Mushroom Mania** to gra eksploracyjna FPS osadzona w prostym środowisku 3D, w której gracz zbiera różne rodzaje grzybów rozmieszczonych w świecie gry. Każdy grzyb ma własną wartość punktową oraz nazwę, a celem jest zdobycie jak największej liczby punktów w określonym limicie czasowym. Dodatkowym utrudnieniem jest patrolujący teren gry strażnik, który pilnuje aby gracz nie zbierał nielegalnie grzybów z lasu.

Projekt skupia się na mechanikach interakcji, systemie punktacji, responsywnym feedbacku wizualnym oraz urozmaiceniem w postaci sztucznej inteligencji oparej o prostą sieć neurnonową.

### Główne Funkcjonalności

* **System Poruszania FPS:** Płynny ruch postaci z kamerą pierwszoosobową.
* **Interakcja z obiektami:** Zbieranie grzybów poprzez raycast i system interakcji.

* **System Punktów:**
  * Każdy grzyb ma przypisaną wartość punktową.
  * Dynamiczny licznik punktów (globalny system score).

* **UI Interaktywne:**
  * Crosshair zmieniający się podczas interakcji.
  * Tekst informacyjny nad obiektem (nazwa grzyba).
  * Popup punktów (+X NazwaGrzyba) z animacją.

* **Respawn System:**
  * Zebrane grzyby znikają i pojawiają się ponownie po określonym czasie.

* **System Strażnika AI:**
  * AI patrolujący mapę, reagujący na obecność gracza w strefie detekcji.
  * Wykorzystanie prostej sieci neuronowej do klasyfikacji stanów zachowania (patrol / chase / investigate).
  * Decyzje podejmowane na podstawie wejść sensorycznych (odległość do gracza, kąt widzenia, czas kontaktu wzrokowego).
  * Dynamiczna zmiana zachowania w zależności od sytuacji w świecie gry.

* **System Sztucznej Inteligencji (Neural AI):**
  * Prosta implementacja sieci neuronowej sterującej zachowaniem strażnika.
  * Warstwa wejściowa: dane środowiskowe (pozycja gracza, dystans, widoczność).
  * Warstwa ukryta: przetwarzanie decyzji behawioralnych.
  * Warstwa wyjściowa: wybór akcji (patrol, pościg, powrót do punktu bazowego).
  * System uczący się heurystycznie poprzez dostrajanie wag (behavior tuning).

* **Feedback System (UX):**
  * Animowane popupy punktów (ruch + fade).
  * Wizualne i dźwiękowe potwierdzenie interakcji.
  * Spójny system komunikacji UI (brak losowych kolorowych oznaczeń — jednolity design systemowy).

### Technologie
* **Silnik:** Unity 6 (wersja 6000.0.24f1)
* **Język:** C# (Logika gry, generowanie lasu, prosta sieć neuronowa - Noedify )
* **Grafika 3D:** Blender (modele, otoczenie)
* **Grafika 2D:** Photoshop / Illustrator (UI, ikony, proste rysunki 2D)

### Instalacja i Uruchomienie

1. Sklonuj repozytorium:
   ```bash
   https://github.com/LukaszMatecki/Mushroom-Mania-Unity.git
3. Otwórz w Unity Hub:
   Wskaż folder z projektem (Wymagana wersja Unity: 6000.0.24f1).
5. Uruchom Grę:
   Otwórz scenę Menu.unity i kliknij przycisk Play.

---

## English Version

## About the Project

**Mushroom Mania** is a first-person exploration game set in a simple 3D environment where the player collects various types of mushrooms scattered across the world. Each mushroom has its own point value and name, and the main objective is to achieve the highest possible score within a limited time frame. An additional challenge is a roaming guardian AI that patrols the forest and prevents the player from illegally harvesting mushrooms.

The project focuses on interaction mechanics, scoring systems, responsive visual feedback, and enhanced gameplay variety through artificial intelligence based on a simple neural network model.

---

## Key Features

* **FPS Movement System:** Smooth first-person character movement with camera control.
* **Object Interaction:** Mushroom collection using raycast-based interaction system.

* **Score System:**
  * Each mushroom has an assigned point value.
  * Global dynamic score tracking system.

* **Interactive UI:**
  * Dynamic crosshair that changes during interaction.
  * On-screen prompt displaying mushroom name.
  * Animated score popup (+X MushroomName).

* **Respawn System:**
  * Collected mushrooms disappear and respawn after a cooldown period.

* **Guardian AI System:**
  * AI-controlled guardian patrolling the map and reacting to player presence within detection zones.
  * Uses a simple neural network for behavior classification (patrol / chase / investigate).
  * Decisions are based on sensory inputs such as player distance, field of view angle, and visibility duration.
  * Dynamic behavior switching depending on environmental conditions.

* **Artificial Intelligence System (Neural AI):**
  * Lightweight neural network controlling guardian behavior.
  * Input layer: environmental data (player position, distance, visibility).
  * Hidden layer: behavioral decision processing.
  * Output layer: action selection (patrol, chase, return to base).
  * Heuristic-based learning system through weight tuning (behavior adjustment over time).

* **Feedback System (UX):**
  * Animated score popups (movement + fade).
  * Visual and audio interaction feedback.
  * Consistent UI communication system (no random color usage, unified design system).

---

## Technologies

* **Engine:** Unity 6 (version 6000.0.24f1)
* **Language:** C# (game logic, procedural forest generation, simple neural network system - Noedify)
* **3D Graphics:** Blender (models, environment)
* **2D Graphics:** Photoshop / Illustrator (UI, icons, 2D assets)

---

## Installation and Setup

1. Clone the repository:
   ```bash
   https://github.com/LukaszMatecki/Mushroom-Mania-Unity.git
3. Open in Unity Hub:
   Select the project folder (Required Unity version: 6000.0.24f1).
4. Play:
   Open Menu.unity and press the Play button.

---

<div align="center">

### Autorzy / Authors

| Role | Name | GitHub |
|:---:|:---:|:---:|
| **Developer & UI/UX Designer** | **Łukasz Matecki** | [GitHub Profile](https://github.com/LukaszMatecki) |
| **Developer**  | **Michał Chlebicz** | [GitHub Profile](https://github.com/Stalk0n) |

<br>
<i>Created for educational purposes.</i>
</div>
