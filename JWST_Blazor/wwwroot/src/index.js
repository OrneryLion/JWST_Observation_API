import { Calendar } from '@fullcalendar/core';
import dayGridPlugin from '@fullcalendar/daygrid';

window.initializeCalendar = function (elementId, events) {
    var calendarEl = document.getElementById(elementId);

    var calendar = new Calendar(calendarEl, {
        plugins: [dayGridPlugin],
        initialView: 'dayGridMonth',
        events: events
    });

    calendar.render();
}
