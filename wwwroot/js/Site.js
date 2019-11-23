/**
 * Authors: David Bruck (dbruck1@@fau.edu) and Freguens Mildort (fmildort2015@@fau.edu)
 * Original source: https://github.com/CDA6122/Project
 * License: BSD 2-Clause License (https://opensource.org/licenses/BSD-2-Clause)
 **/
// This file is only necessary because BlazorServer currently has a bug modifying svg foreignObject children
(function () {
    var previousEvent, nextEvent, currentEventNumber, eventText;
    function getElements(repeat) {
        previousEvent = document.getElementById('previousEvent');
        if (!previousEvent) {
            setTimeout(repeat);
            return;
        }
        nextEvent = document.getElementById('nextEvent');
        currentEventNumber = document.getElementById('currentEventNumber');
        eventText = document.getElementById('eventText');
        return true;
    }

    function setEventTextRunning(val) {
        document.getElementById('eventTextRunning').innerHTML = val;
    }

    function disablePreviousEvent() {
        if (!previousEvent && !getElements(disablePreviousEvent)) {
            return;
        }
        previousEvent.setAttribute('disabled', 'disabled');
    }

    function enablePreviousEvent() {
        previousEvent.removeAttribute('disabled');
    }

    function disableNextEvent() {
        if (!nextEvent && !getElements(disableNextEvent)) {
            return;
        }
        nextEvent.setAttribute('disabled', 'disabled');
    }

    function enableNextEvent() {
        nextEvent.removeAttribute('disabled');
    }

    function setCurrentEventNumber(val) {
        if (!currentEventNumber && !getElements(setCurrentEventNumber.bind(this, val))) {
            return;
        }
        const currentEventNumberChildren = currentEventNumber.childNodes;
        if (currentEventNumberChildren) {
            for (let currentEventNumberChildIdx = 0; currentEventNumberChildIdx < currentEventNumberChildren.length; currentEventNumberChildIdx++) {
                currentEventNumber.removeChild(currentEventNumberChildren[currentEventNumberChildIdx]);
            }
        }
        currentEventNumber.appendChild(document.createTextNode(val));
    }

    function setEventText(val) {
        if (!eventText && !getElements(setEventText.bind(this, val))) {
            return;
        }
        eventText.innerHTML = val;
    }

    function bindPreviousNextEventButtons(dotnetHelper) {
        if (!previousEvent && !getElements(bindPreviousNextEventButtons.bind(this, dotnetHelper))) {
            return;
        }
        previousEvent.addEventListener('click', dotnetHelper.invokeMethodAsync.bind(dotnetHelper, 'OnPreviousEvent'));
        nextEvent.addEventListener('click', dotnetHelper.invokeMethodAsync.bind(dotnetHelper, 'OnNextEvent'));
    }

    // exports:
    window.setEventTextRunning = setEventTextRunning;
    window.disablePreviousEvent = disablePreviousEvent;
    window.enablePreviousEvent = enablePreviousEvent;
    window.disableNextEvent = disableNextEvent;
    window.enableNextEvent = enableNextEvent;
    window.setCurrentEventNumber = setCurrentEventNumber;
    window.setEventText = setEventText;
    window.bindPreviousNextEventButtons = bindPreviousNextEventButtons;
})();